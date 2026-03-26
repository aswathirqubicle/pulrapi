using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using Core.Application.Models.Search;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetProductsSearchQuery : IRequest<PaginatedResultDto<ProductSearchResult>>
    {
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int? PageSize { get; set; }
    }

    public class GetProductsSearchQueryHandler : IRequestHandler<GetProductsSearchQuery, PaginatedResultDto<ProductSearchResult>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private const int DefaultPageSize = 10;

        public GetProductsSearchQueryHandler(IApplicationDbContext dbContext, IMediator mediator, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mediator = mediator;
            _configuration = configuration;
        }

        public async Task<PaginatedResultDto<ProductSearchResult>> Handle(GetProductsSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate and set default values for pagination
                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize ?? DefaultPageSize;
                if (pageSize <= 0) pageSize = DefaultPageSize;

                var searchTerm = request.SearchTerm?.ToLower() ?? string.Empty;

                // Create the base query
                var baseQuery = _dbContext.Products
                    .Include(p => p.Country)
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Where(p => p.IsActive
                                && (p.Name.ToLower().Contains(searchTerm) ||
                                    p.WhatIsIt.ToLower().Contains(searchTerm) ||
                                    (p.Brand != null && p.Brand.ToLower().Contains(searchTerm))));

                // Get total count
                var totalCount = await baseQuery.CountAsync(cancellationToken);

                // Get the items
                var productEntities = await baseQuery
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Get active product IDs that have processing orders
                var productIds = productEntities.Select(p => p.Id).ToList();
                var processingProductIds = await _dbContext.OrderProductAffiliates
                    .Where(opa => productIds.Contains(opa.ProductId) && 
                                 (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                                  opa.Order.OrderStatus == OrderStatusEnum.Processing))
                    .Select(opa => opa.ProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var items = productEntities.Select(p => new ProductSearchResult
                {
                    ProductUid = p.Uid,
                    IsDeletable = !processingProductIds.Contains(p.Id),
                    ProductName = p.Name,
                    ProductImageUrl = p.ProductMediaFiles.OrderBy(mf => mf.MediaFile.Priority)
                        .Select(mf => mf.MediaFile.Url).FirstOrDefault(),
                    WhatIsIt = p.WhatIsIt,
                    Brand = p.Brand,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice,
                    CountryCode = p.Country != null ? p.Country.Iso3 : _configuration["DefaultCountryCode"],
                    CurrencyCode = p.Country != null ? p.Country.Iso4 : _configuration["DefaultCurrencyCode"]
                }).ToList();

                // Create paginated result
                var result = PaginatedResultDto<ProductSearchResult>.Create(page, pageSize, totalCount, items);

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while searching products", e);
            }
        }
    }
}
