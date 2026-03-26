using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.BagItems.Queries;
using Core.Application.Models.BagItems;
using Core.Application.Models.Currencies;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.BagItems.Queries
{
    public class GetBagItemsQuery : IRequest<BagResponse>
    {
    }

    public class GetBagItemsQueryHandler : IRequestHandler<GetBagItemsQuery, BagResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetBagItemsQueryHandler> _logger;

        public GetBagItemsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            ILogger<GetBagItemsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<BagResponse> Handle(GetBagItemsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(false);
                var myBagResponse = new BagResponse();

                if (cUser == null)
                    return myBagResponse;

                myBagResponse.Currency = await _dbContext.GlobalCurrencySettings.Select(gcs =>
                    new CurrencyDetailsResponse
                    {
                        Code = gcs.BaseCurrency.Code,
                        Name = gcs.BaseCurrency.Name,
                        Symbol = gcs.BaseCurrency.Symbol,
                        Uid = gcs.BaseCurrency.Uid,
                    }).SingleOrDefaultAsync(cancellationToken);


                if (cUser.Profile.Currency != null)
                {
                    myBagResponse.Currency = new CurrencyDetailsResponse
                    {
                        Code = cUser.Profile.Currency.Code,
                        Name = cUser.Profile.Currency.Name,
                        Symbol = cUser.Profile.Currency.Symbol,
                        Uid = cUser.Profile.Currency.Uid,
                    };
                }

                var anyProducts = await _dbContext.UserBagProducts.Where(bp => bp.UserId == cUser.Id)
                    .AnyAsync(cancellationToken);
                if (!anyProducts)
                {
                    return myBagResponse;
                }

                var bagItems = await _dbContext.UserBagProducts
                    .Where(bp => bp.UserId == cUser.Id)
                    .Include(bp => bp.BagProduct)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                    .Include(bp => bp.BagProduct)
                        .ThenInclude(p => p.ProductVariant)
                            .ThenInclude(pv => pv.ProductVariantOptions)
                    .Include(bp => bp.BagProduct)
                        .ThenInclude(p => p.ProductVariantCombinations)
                            .ThenInclude(pvc => pvc.CombinationOptions)
                                .ThenInclude(co => co.ProductVariantOption)
                                    .ThenInclude(pvo => pvo.ProductVariant)
                    .Include(bp => bp.BagProduct)
                        .ThenInclude(p => p.User)
                    .Include(bp => bp.ProductVariantCombination)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .OrderByDescending(bp => bp.CreatedAt)
                    .ToListAsync(cancellationToken);

                // Get unique seller user IDs from products
                var sellerUserIds = bagItems
                    .Where(bp => bp.BagProduct?.UserId != null)
                    .Select(bp => bp.BagProduct.UserId)
                    .Distinct()
                    .ToList();

                // Load SellerSettings for all unique sellers
                var sellerSettingsDict = await _dbContext.SellerSettings
                    .Where(ss => sellerUserIds.Contains(ss.UserId))
                    .ToDictionaryAsync(ss => ss.UserId, ss => ss, cancellationToken);

                // Calculate total shipping cost (sum of unique sellers' shipping costs)
                // If multiple products from same seller, count shipping cost only once per seller
                // If multiple sellers, sum all their shipping costs
                myBagResponse.TotalShippingCost = sellerUserIds.Sum(sellerId => 
                {
                    if (sellerSettingsDict.TryGetValue(sellerId, out var ss))
                    {
                        return ss.ShippingCosts ?? 0;
                    }
                    return 0; // Default shipping cost is 0 if seller hasn't set it
                });

                // Get all wishlist items for the user to check InWishlist flag
                var wishlistItems = await _dbContext.UserWishlistProducts
                    .Where(w => w.UserId == cUser.Id)
                    .Select(w => new 
                    { 
                        ProductId = w.WishlistProductId, 
                        VariantCombinationUid = w.ProductVariantCombinationUid 
                    })
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

                myBagResponse.Products = bagItems.Select(bp =>
                {
                    var product = bp.BagProduct;
                    var variantCombination = bp.ProductVariantCombination;

                    // Get seller settings for this product's seller
                    SellerSettings sellerSettings = null;
                    
                    if (product?.UserId != null)
                    {
                        sellerSettingsDict.TryGetValue(product.UserId, out sellerSettings);
                    }

                    var response = new BagProductResponse
                    {
                        BagQuantity = bp.Quantity,
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
                        ProductVariantCombinationUid = bp.ProductVariantCombinationUid,
                        Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null),
                        ShippingCost = sellerSettings?.ShippingCosts ?? 0,
                        DeliveryTime = sellerSettings?.DeliveryTime
                    };

                    // Extract size and color from variant combination
                    if (variantCombination != null && variantCombination.CombinationOptions.Any())
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

                    if (variantCombination != null && variantCombination.CombinationOptions != null)
                    {
                        response.ProductVariants = variantCombination.CombinationOptions
                            .Where(co => co.ProductVariantOption?.ProductVariant != null)
                            .Select(co => new ProductVariantResponse
                            {
                                VariantName = co.ProductVariantOption.ProductVariant.VariantName,
                                VariantOptions = new List<string> { co.ProductVariantOption.Value }
                            })
                            .ToList();

                        var combinationKey = variantCombination.CombinationOptions
                            .FirstOrDefault()?.ProductVariantOption?.Value ?? "Default";

                        response.SelectedProductVariantCombination = BuildCombinationResponse(variantCombination);
                    }
                    else if (product.ProductVariant != null)
                    {
                        response.ProductVariants = product.ProductVariant
                            ?.Where(pv =>
                            {
                                var isStandardVariant =
                                    pv.VariantName.Equals("standard", StringComparison.OrdinalIgnoreCase);
                                var options = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ??
                                              new List<string>();
                                var hasOnlyProductNameOption = options.Count == 1 &&
                                    options.Any(opt =>
                                        opt.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
                                return !(isStandardVariant && hasOnlyProductNameOption);
                            })
                            .Select(pv => new ProductVariantResponse
                            {
                                VariantName = pv.VariantName,
                                VariantOptions = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ??
                                                new List<string>()
                            }).ToList() ?? new List<ProductVariantResponse>();
                    }

                    response.SelectedProductVariantCombination ??= BuildCombinationResponse(
                        product.ProductVariantCombinations?
                            .FirstOrDefault(pvc => pvc.IsActive));

                    // Check if this product is in the wishlist
                    response.InWishlist = wishlistItems.Any(w => 
                        w.ProductId == product.Id && 
                        (string.IsNullOrEmpty(bp.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(w.VariantCombinationUid)
                            : w.VariantCombinationUid == bp.ProductVariantCombinationUid));

                    return response;
                }).ToList();

                return myBagResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
