using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Categories.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetProductDetailsQuery : IRequest<ProductDetailsResponse>
    {
        [Required] public string Uid { get; set; }
        public string CurrencyCode { get; set; }
        public string AffiliateId { get; set; }
    }

    public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, ProductDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProductDetailsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetProductDetailsQueryHandler(
            IApplicationDbContext dbContext,
            ILogger<GetProductDetailsQueryHandler> logger,
            ICurrentUserService currentUserService
            )
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<ProductDetailsResponse> Handle(GetProductDetailsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get current user if authenticated (optional for product details)
                var currentUser = await _currentUserService.GetUserAsync(false);

                // First get the product entity with all related data
                var productEntity = await _dbContext.Products
                    .AsNoTracking()
                    .Include(p => p.User)
                        .ThenInclude(u => u.Profile)
                    .Include(p => p.Country)
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariant)
                        .ThenInclude(pv => pv.ProductVariantOptions)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(co => co.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .Where(p => p.IsActive && p.Uid == request.Uid)
                    .FirstOrDefaultAsync(cancellationToken);

                if (productEntity == null)
                    return null;

                // Check if product is in user's wishlist
                bool inWishlist = false;
                if (currentUser != null)
                {
                    inWishlist = await _dbContext.UserWishlistProducts
                        .AnyAsync(w => w.UserId == currentUser.Id && w.WishlistProductId == productEntity.Id, 
                            cancellationToken);
                }

                // Check if product has active orders
                bool isDeletable = !await _dbContext.OrderProductAffiliates
                    .AnyAsync(opa => opa.ProductId == productEntity.Id && 
                                   (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                                    opa.Order.OrderStatus == OrderStatusEnum.Processing), 
                        cancellationToken);

                // Then map to response in memory to avoid LINQ translation issues
                var product = new ProductDetailsResponse
                {
                    Uid = productEntity.Uid,
                    IsDeletable = isDeletable,
                    Name = productEntity.Name,
                    WhatIsIt = productEntity.WhatIsIt,
                    ProductDetail = productEntity.ProductDetail,
                    Brand = productEntity.Brand,
                    MinPrice = productEntity.MinPrice,
                    MaxPrice = productEntity.MaxPrice,
                    CountryCode = productEntity.Country?.Iso2,
                    CurrencyCode = productEntity.Country?.Iso4,
                    ProductUrl = productEntity.ProductUrl,
                    Type = productEntity.Type,
                    SellType = productEntity.SellType,
                    InWishlist = inWishlist,
                    ProductMediaFiles = productEntity.ProductMediaFiles
                        .Where(pm => pm.MediaFile.IsActive)
                        .Select(pm => new MediaFileDetailsResponse
                        {
                            Uid = pm.MediaFile.Uid,
                            Url = pm.MediaFile.Url,
                            FileType = pm.MediaFile.MediaFileType.ToString(),
                            Priority = pm.MediaFile.Priority,
                            IsHlsProcessed = pm.MediaFile.IsHlsProcessed,
                            OriginalUrl = pm.MediaFile.OriginalUrl,
                            HlsBasePath = pm.MediaFile.HlsBasePath,
                            VideoDurationSeconds = pm.MediaFile.VideoDurationSeconds,
                            AvailableQualities = pm.MediaFile.AvailableQualities
                        }).ToList(),
                    ProductVariants = productEntity.ProductVariant?.Where(pv => 
                        {
                            // Hide variants if it's a "standard" variant with only the product name as option
                            var isStandardVariant = pv.VariantName.Equals("standard", StringComparison.OrdinalIgnoreCase);
                            var options = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>();
                            var hasOnlyProductNameOption = options.Count == 1 && 
                                options.Any(opt => opt.Equals(productEntity.Name, StringComparison.OrdinalIgnoreCase));
                            
                            // Return false to filter out (hide) standard variants with only product name
                            return !(isStandardVariant && hasOnlyProductNameOption);
                        })
                        .Select(pv => new ProductVariantResponse
                        {
                            VariantName = pv.VariantName,
                            VariantOptions = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>()
                        }).ToList() ?? new List<ProductVariantResponse>(),
                    ProductVariantCombinations = (productEntity.ProductVariantCombinations?
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
                    CreatedAt = productEntity.CreatedAt,
                    UpdatedAt = productEntity.UpdatedAt,
                    Profile = productEntity.User?.Profile != null ? new ProfileBaseResponse
                    {
                        Uid = productEntity.User.Profile.Uid,
                        UserId = productEntity.User.Id,
                        ImageUrl = productEntity.User.Profile.ImageUrl,
                        FullName = productEntity.User.FirstName,
                        FirstName = productEntity.User.FirstName,
                        LastName = productEntity.User.LastName,
                        Username = productEntity.User.UserName,
                        DisplayName = productEntity.User.DisplayName,
                        UserType = productEntity.User.Profile.UserType,
                        FollowedByMe = false
                    } : null
                };

                if (product == null)
                {
                    throw new BadRequestException("Product doesn't exist.");
                }

                return product;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
