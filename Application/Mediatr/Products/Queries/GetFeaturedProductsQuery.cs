using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetFeaturedProductsQuery : PagingParamsRequest, IRequest<FeaturedProductsResponse>
    {
        // Returns both Hot-Seller and New-In products in a single response
    }

    public class GetFeaturedProductsQueryHandler : IRequestHandler<GetFeaturedProductsQuery, FeaturedProductsResponse>
    {
        private readonly ILogger<GetFeaturedProductsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public GetFeaturedProductsQueryHandler(
            ILogger<GetFeaturedProductsQueryHandler> logger,
            IApplicationDbContext dbContext,
            IQueryHelperService queryHelperService,
            IExchangeRateService exchangeRateService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _queryHelperService = queryHelperService;
            _exchangeRateService = exchangeRateService;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<FeaturedProductsResponse> Handle(GetFeaturedProductsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get Hot-Seller Products
                var hotSellerProducts = await GetHotSellerProducts(request, cancellationToken);
                
                // Get New-In Products
                var newInProducts = await GetNewInProducts(request, cancellationToken);

                return new FeaturedProductsResponse
                {
                    HotSellerProducts = hotSellerProducts,
                    NewInProducts = newInProducts
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<PagingResponse<ProductPublicResponse>> GetHotSellerProducts(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
        {
            // Hot-selling: highest tagged + followers count
            // First get the product IDs with their scores
            var productScores = await _dbContext.Products
                .Where(p => p.IsActive == true)
                .Select(p => new
                {
                    ProductId = p.Id,
                    TagCount = _dbContext.PostProductTags.Count(ppt => ppt.ProductId == p.Id),
                    FollowerCount = p.User.Profile.ProfileFollowers.Count
                })
                .ToListAsync(cancellationToken);

            var orderedProductIds = productScores
                .OrderByDescending(x => x.TagCount + x.FollowerCount)
                .ThenByDescending(x => x.TagCount)
                .ThenByDescending(x => x.FollowerCount)
                .Select(x => x.ProductId)
                .ToList();

            // Build the main query with all necessary includes
            IQueryable<Product> query = _dbContext.Products
                .Include(p => p.User)
                    .ThenInclude(u => u.Profile)
                .Include(p => p.Country)
                .Include(p => p.ProductMediaFiles)
                    .ThenInclude(pmf => pmf.MediaFile)
                .Include(p => p.ProductVariant)
                    .ThenInclude(pv => pv.ProductVariantOptions)
                .Where(e => e.IsActive == true)
                .Where(p => orderedProductIds.Contains(p.Id))
                .OrderBy(p => orderedProductIds.IndexOf(p.Id));

            var list = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);
            return await MapProductsToResponse(list, cancellationToken);
        }

        private async Task<PagingResponse<ProductPublicResponse>> GetNewInProducts(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
        {
            // New-in: new products from top followers (users with most followers)
            var topFollowerUserIds = await _dbContext.Profiles
                .Select(p => new
                {
                    UserId = p.UserId,
                    FollowerCount = p.ProfileFollowers.Count
                })
                .OrderByDescending(x => x.FollowerCount)
                .Take(100) // Get top 100 users by follower count
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);

            // Build the main query for new products from top followers
            IQueryable<Product> query = _dbContext.Products
                .Include(p => p.User)
                    .ThenInclude(u => u.Profile)
                .Include(p => p.Country)
                .Include(p => p.ProductMediaFiles)
                    .ThenInclude(pmf => pmf.MediaFile)
                .Include(p => p.ProductVariant)
                    .ThenInclude(pv => pv.ProductVariantOptions)
                .Where(e => e.IsActive == true)
                .Where(p => topFollowerUserIds.Contains(p.UserId))
                .OrderByDescending(p => p.CreatedAt);

            var list = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);
            return await MapProductsToResponse(list, cancellationToken);
        }

        private async Task<PagingResponse<ProductPublicResponse>> MapProductsToResponse(PagedList<Product> list, CancellationToken cancellationToken)
        {
            var mappedList = _mapper.Map<PagingResponse<ProductPublicResponse>>(list);

            // Get active product IDs that have processing orders
            var pagedProductIds = list.Select(p => p.Id).ToList();
            var processingProductIds = await _dbContext.OrderProductAffiliates
                .Where(opa => pagedProductIds.Contains(opa.ProductId) && 
                             (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                              opa.Order.OrderStatus == OrderStatusEnum.Processing))
                .Select(opa => opa.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Add profile information to each product
            for (int i = 0; i < list.Count; i++)
            {
                var product = list[i];
                var item = mappedList.Items[i];

                item.IsDeletable = !processingProductIds.Contains(product.Id);

                if (product?.ProductVariant != null)
                {
                    item.ProductVariants = product.ProductVariant
                        .Select(variant => new ProductVariantResponse
                        {
                            VariantName = variant.VariantName,
                            VariantOptions = variant.ProductVariantOptions?.Select(opt => opt.Value).ToList() ?? new List<string>()
                        })
                        .ToList();
                }

                if (product?.User?.Profile != null)
                {
                    item.Profile = new ProfileBaseResponse
                    {
                        Uid = product.User.Profile.Uid,
                        UserId = product.User.Id,
                        ImageUrl = product.User.Profile.ImageUrl,
                        //IsStore = false,
                        FullName = product.User.FirstName,
                        LastName = product.User.LastName,
                        Username = product.User.UserName,
                        DisplayName = product.User.DisplayName,
                        UserType = product.User.Profile.UserType,
                        FollowedByMe = false // This would need to be calculated based on current user
                    };
                }

                // Currency information is now mapped directly from Country via AutoMapper
            }

            mappedList.ItemIds = mappedList.Items.Select(item => item.Uid).ToList();
            return mappedList;
        }
    }
}
