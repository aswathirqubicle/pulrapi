using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.BagItems;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Wishlist.Commands
{
    public class MoveFromWishlistToBagCommand : IRequest<BagProductResponse>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class MoveFromWishlistToBagCommandHandler : IRequestHandler<MoveFromWishlistToBagCommand, BagProductResponse>
    {
        private readonly ILogger<MoveFromWishlistToBagCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public MoveFromWishlistToBagCommandHandler(
            ILogger<MoveFromWishlistToBagCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<BagProductResponse> Handle(MoveFromWishlistToBagCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(true);
                if (user == null)
                {
                    throw new Core.Application.Exceptions.NotAuthenticatedException("User not authenticated");
                }

                // Find product
                var product = await _dbContext.Products
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                    .FirstOrDefaultAsync(p => p.Uid == request.ProductUid, cancellationToken);

                if (product == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found");
                }

                // Find wishlist item
                var wishlistItem = await _dbContext.UserWishlistProducts
                    .FirstOrDefaultAsync(w => w.UserId == user.Id 
                        && w.WishlistProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(w.ProductVariantCombinationUid)
                            : w.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                if (wishlistItem == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found in wishlist");
                }

                // Check if already in bag
                var existingBagItem = await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                ProductVariantCombination variantCombination = null;
                if (!string.IsNullOrEmpty(request.ProductVariantCombinationUid))
                {
                    variantCombination = await _dbContext.ProductVariantCombinations
                        .Include(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                        .FirstOrDefaultAsync(pvc => pvc.Uid == request.ProductVariantCombinationUid 
                            && pvc.ProductId == product.Id, cancellationToken);
                }

                if (existingBagItem != null)
                {
                    // Update quantity
                    existingBagItem.Quantity += request.Quantity;
                }
                else
                {
                    // Create new bag item
                    var bagItem = new UserBagProduct
                    {
                        BagProduct = product,
                        User = user,
                        Quantity = request.Quantity,
                        ProductVariantCombinationUid = request.ProductVariantCombinationUid,
                        ProductVariantCombination = variantCombination
                    };

                    _dbContext.UserBagProducts.Add(bagItem);
                }

                // Remove from wishlist
                _dbContext.UserWishlistProducts.Remove(wishlistItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Build response
                var bagItemForResponse = existingBagItem ?? await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                var response = new BagProductResponse
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
                    BagQuantity = bagItemForResponse?.Quantity ?? request.Quantity,
                    ProductVariantCombinationUid = request.ProductVariantCombinationUid,
                    Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null)
                };

                // Extract size and color
                if (variantCombination != null && variantCombination.CombinationOptions.Any())
                {
                    var options = variantCombination.CombinationOptions
                        .Select(cvo => cvo.ProductVariantOption)
                        .ToList();

                    var sizeOption = options.FirstOrDefault(o => o.ProductVariant?.VariantName?.ToLower() == "size");
                    var colorOption = options.FirstOrDefault(o => o.ProductVariant?.VariantName?.ToLower() == "color");

                    response.Size = sizeOption?.Value;
                    response.Color = colorOption?.Value;
                }

                // Map media files
                if (product.ProductMediaFiles != null && product.ProductMediaFiles.Any())
                {
                    response.ProductMediaFiles = product.ProductMediaFiles
                        .OrderBy(pmf => pmf.MediaFile.Priority)
                        .Select(pmf => new Core.Application.Models.MediaFiles.MediaFileDetailsResponse
                        {
                            Uid = pmf.MediaFile.Uid,
                            Url = pmf.MediaFile.Url,
                            FileType = pmf.MediaFile.MediaFileType.ToString(),
                            Priority = pmf.MediaFile.Priority
                        })
                        .ToList();
                }

                // Map selected variant combination
                if (variantCombination != null)
                {
                    var displayName = variantCombination.CombinationOptions?
                        .Select(co => co.ProductVariantOption?.Value)
                        .Where(v => !string.IsNullOrEmpty(v))
                        .ToList();

                    response.SelectedProductVariantCombination = new Core.Application.Models.Products.ProductVariantCombinationResponse
                    {
                        Uid = variantCombination.Uid,
                        SKU = variantCombination.SKU,
                        Price = variantCombination.Price,
                        Quantity = variantCombination.Quantity,
                        ImageUrl = variantCombination.ImageUrl,
                        IsAvailable = variantCombination.IsAvailable,
                        DisplayName = displayName != null && displayName.Any() ? string.Join(", ", displayName) : null,
                        VariantValues = variantCombination.CombinationOptions?
                            .OrderBy(co => co.ProductVariantOption?.ProductVariant?.Id ?? 0)
                            .Select(co => co.ProductVariantOption?.Value)
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToArray() ?? Array.Empty<string>()
                    };
                }

                // Set InWishlist to false since we're removing from wishlist
                response.InWishlist = false;

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error moving product from wishlist to bag: {Message}", e.Message);
                throw;
            }
        }
    }
}

