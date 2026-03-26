using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetUserProductsQuery : PagingParamsRequest, IRequest<PagingResponse<ProductDetailsResponse>>
    {
        public new string Search { get; set; }
        public ProductTypeEnum? Type { get; set; }
    }

    public class GetUserProductsQueryHandler : IRequestHandler<GetUserProductsQuery, PagingResponse<ProductDetailsResponse>>
    {
        private readonly ILogger<GetUserProductsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetUserProductsQueryHandler(
            ILogger<GetUserProductsQueryHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<ProductDetailsResponse>> Handle(GetUserProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Build query to get user's active products
                IQueryable<Product> query = _dbContext.Products
                    .AsNoTracking()
                    .Include(p => p.Country)
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariant)
                        .ThenInclude(pv => pv.ProductVariantOptions)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(co => co.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .Where(p => p.UserId == user.Id && p.IsActive);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var searchTerm = request.Search.ToLower().Trim();
                    query = query.Where(p => 
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.WhatIsIt.ToLower().Contains(searchTerm) ||
                        (p.ProductDetail != null && p.ProductDetail.ToLower().Contains(searchTerm)) ||
                        (p.Brand != null && p.Brand.ToLower().Contains(searchTerm))
                    );
                }

                // Filter by product type if provided
                if (request.Type.HasValue)
                {
                    query = query.Where(p => p.Type == request.Type.Value);
                }

                // Apply ordering
                if (string.IsNullOrWhiteSpace(request.Order) || string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
                else
                {
                    switch (request.OrderBy.ToLower())
                    {
                        case "name":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name);
                            break;
                        case "price":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.MinPrice) : query.OrderByDescending(p => p.MinPrice);
                            break;
                        case "createdat":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                            break;
                        default:
                            query = query.OrderByDescending(p => p.CreatedAt);
                            break;
                    }
                }

                // Get paginated list first
                var pagedList = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);

                // Get active product IDs that have processing orders
                var pagedProductIds = pagedList.Select(p => p.Id).ToList();
                var processingProductIds = await _dbContext.OrderProductAffiliates
                    .Where(opa => pagedProductIds.Contains(opa.ProductId) && 
                                 (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                                  opa.Order.OrderStatus == OrderStatusEnum.Processing))
                    .Select(opa => opa.ProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Map to response in memory to avoid LINQ translation issues
                var queryMapped = pagedList.Select(p => new ProductDetailsResponse
                {
                    Uid = p.Uid,
                    IsDeletable = !processingProductIds.Contains(p.Id),
                    Name = p.Name,
                    WhatIsIt = p.WhatIsIt,
                    ProductDetail = p.ProductDetail,
                    Brand = p.Brand ?? null,
                    MinPrice = p.MinPrice,
                    MaxPrice = p.MaxPrice,
                    CountryCode = p.Country?.Iso2,
                    CurrencyCode = p.Country?.Iso4,
                    ProductUrl = p.ProductUrl,
                    Type = p.Type,
                    SellType = p.SellType,
                    ProductMediaFiles = p.ProductMediaFiles
                        .Where(pmf => pmf.MediaFile.IsActive)
                        .Select(pmf => new MediaFileDetailsResponse
                        {
                            Uid = pmf.MediaFile.Uid,
                            FileType = pmf.MediaFile.MediaFileType.ToString(),
                            Url = pmf.MediaFile.Url
                        }).ToList(),
                    ProductVariants = p.ProductVariant?.Where(pv => 
                        {
                            // Hide variants if it's a "standard" variant with only the product name as option
                            var isStandardVariant = pv.VariantName.Equals("standard", StringComparison.OrdinalIgnoreCase);
                            var options = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>();
                            var hasOnlyProductNameOption = options.Count == 1 && 
                                options.Any(opt => opt.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                            
                            // Return false to filter out (hide) standard variants with only product name
                            return !(isStandardVariant && hasOnlyProductNameOption);
                        })
                        .Select(pv => new ProductVariantResponse
                        {
                            VariantName = pv.VariantName,
                            VariantOptions = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>()
                        }).ToList() ?? new List<ProductVariantResponse>(),
                    ProductVariantCombinations = (p.ProductVariantCombinations?
                        .Where(pvc => pvc.IsActive)
                        .GroupBy(pvc => pvc.CombinationOptions.FirstOrDefault()?.ProductVariantOption?.Value ?? "Default")
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(pvc => new ProductVariantCombinationResponse
                            {
                                Uid = pvc.Uid,
                                SKU = pvc.SKU,
                                Price = pvc.Price,
                                Quantity = pvc.Quantity,
                                ImageUrl = pvc.ImageUrl,
                                IsAvailable = pvc.IsAvailable,
                                DisplayName = string.Join(", ", pvc.CombinationOptions.Select(co => co.ProductVariantOption.Value)),
                                VariantValues = pvc.CombinationOptions
                                    .OrderBy(co => co.ProductVariantOption.ProductVariant.Id)
                                    .Select(co => co.ProductVariantOption.Value)
                                    .ToArray()
                            }).ToList()
                        )) ?? new Dictionary<string, List<ProductVariantCombinationResponse>>(),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Profile = new ProfileBaseResponse
                    {
                        Uid = user.Profile.Uid,
                        UserId = user.Id,
                        ImageUrl = user.Profile.ImageUrl,
                        FullName = user.FirstName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.UserName,
                        DisplayName = user.DisplayName,
                        UserType = user.Profile.UserType,
                        FollowedByMe = false
                    }
                }).ToList();

                var response = new PagingResponse<ProductDetailsResponse>
                {
                    Items = queryMapped,
                    CurrentPage = pagedList.CurrentPage,
                    PageSize = pagedList.PageSize,
                    TotalCount = pagedList.TotalCount,
                    TotalPages = pagedList.TotalPages,
                    ItemIds = queryMapped.Select(item => item.Uid).ToList()
                };
                
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}