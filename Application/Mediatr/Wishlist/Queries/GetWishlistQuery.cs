using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Currencies;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Wishlist;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Wishlist.Queries
{
    public class GetWishlistQuery : IRequest<WishlistResponse>
    {
    }

    public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetWishlistQueryHandler> _logger;

        public GetWishlistQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            ILogger<GetWishlistQueryHandler> logger)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<WishlistResponse> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(false);
                var wishlistResponse = new WishlistResponse();

                if (user == null)
                {
                    return wishlistResponse;
                }

                // Get user's currency preference
                wishlistResponse.Currency = await _dbContext.GlobalCurrencySettings.Select(gcs =>
                    new CurrencyDetailsResponse
                    {
                        Code = gcs.BaseCurrency.Code,
                        Name = gcs.BaseCurrency.Name,
                        Symbol = gcs.BaseCurrency.Symbol,
                        Uid = gcs.BaseCurrency.Uid,
                    }).SingleOrDefaultAsync(cancellationToken);

                if (user.Profile?.Currency != null)
                {
                    wishlistResponse.Currency = new CurrencyDetailsResponse
                    {
                        Code = user.Profile.Currency.Code,
                        Name = user.Profile.Currency.Name,
                        Symbol = user.Profile.Currency.Symbol,
                        Uid = user.Profile.Currency.Uid,
                    };
                }

                // Check if user has any wishlist items
                var hasWishlistItems = await _dbContext.UserWishlistProducts
                    .AnyAsync(w => w.UserId == user.Id, cancellationToken);

                if (!hasWishlistItems)
                {
                    return wishlistResponse;
                }

                // Get wishlist items with related data
                var wishlistItems = await _dbContext.UserWishlistProducts
                    .Where(w => w.UserId == user.Id)
                    .Include(w => w.WishlistProduct)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                    .Include(w => w.WishlistProduct)
                        .ThenInclude(p => p.ProductVariant)
                            .ThenInclude(pv => pv.ProductVariantOptions)
                    .Include(w => w.WishlistProduct)
                        .ThenInclude(p => p.ProductVariantCombinations)
                            .ThenInclude(pvc => pvc.CombinationOptions)
                                .ThenInclude(co => co.ProductVariantOption)
                                    .ThenInclude(pvo => pvo.ProductVariant)
                    .Include(w => w.WishlistProduct)
                        .ThenInclude(p => p.User)
                            .ThenInclude(u => u.Profile)
                    .Include(w => w.ProductVariantCombination)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync(cancellationToken);

                ProductVariantCombinationResponse BuildCombinationResponse(ProductVariantCombination combination)
                {
                    if (combination == null)
                    {
                        return null;
                    }

                    var combinationOptions = combination.CombinationOptions?
                        .Where(co => co?.ProductVariantOption != null)
                        .ToList() ?? new List<ProductVariantCombinationOption>();

                    return new ProductVariantCombinationResponse
                    {
                        Uid = combination.Uid,
                        SKU = combination.SKU,
                        Price = combination.Price,
                        Quantity = combination.Quantity,
                        ImageUrl = combination.ImageUrl,
                        IsAvailable = combination.IsAvailable,
                        DisplayName = string.Join(", ",
                            combinationOptions.Select(co => co.ProductVariantOption.Value)),
                        VariantValues = combinationOptions
                            .OrderBy(co => co.ProductVariantOption.ProductVariant?.Id ?? 0)
                            .Select(co => co.ProductVariantOption.Value)
                            .ToArray()
                    };
                }

                wishlistResponse.Products = wishlistItems.Select(w => 
                {
                    var product = w.WishlistProduct;
                    var variantCombination = w.ProductVariantCombination;

                    var response = new WishlistProductResponse
                    {
                        Uid = product.Uid,
                        Name = product.Name,
                        WhatIsIt = product.WhatIsIt,
                        ProductDetail = product.ProductDetail,
                        Brand = product.Brand,
                        MinPrice = product.MinPrice,
                        MaxPrice = product.MaxPrice,
                        ProductUrl = product.ProductUrl,
                        Type = product.Type,
                        CreatedAt = product.CreatedAt,
                        UpdatedAt = product.UpdatedAt,
                        ProductVariantCombinationUid = w.ProductVariantCombinationUid,
                        Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null)
                    };

                    // Extract size and color from variant combination
                    if (variantCombination?.CombinationOptions != null && variantCombination.CombinationOptions.Any())
                    {
                        var options = variantCombination.CombinationOptions
                            .Select(cvo => cvo.ProductVariantOption)
                            .ToList();

                        var sizeOption = options.FirstOrDefault(o => 
                            o.ProductVariant?.VariantName?.ToLower() == "size");
                        var colorOption = options.FirstOrDefault(o => 
                            o.ProductVariant?.VariantName?.ToLower() == "color");

                        response.Size = sizeOption?.Value;
                        response.Color = colorOption?.Value;
                    }

                    // Map media files
                    if (product.ProductMediaFiles != null && product.ProductMediaFiles.Any())
                    {
                        response.ProductMediaFiles = product.ProductMediaFiles
                            .OrderBy(pmf => pmf.MediaFile.Priority)
                            .Select(pmf => new MediaFileDetailsResponse
                            {
                                Uid = pmf.MediaFile.Uid,
                                Url = pmf.MediaFile.Url,
                                FileType = pmf.MediaFile.MediaFileType.ToString(),
                                Priority = pmf.MediaFile.Priority,
                                IsHlsProcessed = pmf.MediaFile.IsHlsProcessed,
                                OriginalUrl = pmf.MediaFile.OriginalUrl,
                                HlsBasePath = pmf.MediaFile.HlsBasePath,
                                VideoDurationSeconds = pmf.MediaFile.VideoDurationSeconds,
                                AvailableQualities = pmf.MediaFile.AvailableQualities
                            })
                            .ToList();
                    }

                    if (product.ProductVariant != null && variantCombination != null)
                    {
                        response.ProductVariants = product.ProductVariant
                            ?.Where(pv => variantCombination.CombinationOptions?
                                    .Any(co => co.ProductVariantOption?.ProductVariant?.Id == pv.Id) == true)
                            .Select(pv => new ProductVariantResponse
                            {
                                VariantName = pv.VariantName,
                                VariantOptions = pv.ProductVariantOptions?
                                    .Where(pvo => variantCombination.CombinationOptions
                                        .Any(co => co.ProductVariantOption?.Id == pvo.Id))
                                    .Select(pvo => pvo.Value)
                                    .ToList() ?? new List<string>()
                            }).ToList() ?? new List<ProductVariantResponse>();
                    }

                    response.SelectedProductVariantCombination = BuildCombinationResponse(variantCombination);

                    // Map profile if available
                    if (product.User?.Profile != null)
                    {
                        response.Profile = new Core.Application.Models.Profiles.ProfileBaseResponse
                        {
                            Uid = product.User.Profile.Uid,
                            ImageUrl = product.User.Profile.ImageUrl,
                            Username = product.User.UserName
                        };
                    }

                    return response;
                }).ToList();

                wishlistResponse.TotalCount = wishlistResponse.Products.Count;

                return wishlistResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting wishlist: {Message}", e.Message);
                throw;
            }
        }
    }
}

