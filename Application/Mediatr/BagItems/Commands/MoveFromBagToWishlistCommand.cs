using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Wishlist;
using Core.Application.Models.Products;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.BagItems.Commands
{
    public class MoveFromBagToWishlistCommand : IRequest<WishlistProductResponse>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
    }

    public class MoveFromBagToWishlistCommandHandler : IRequestHandler<MoveFromBagToWishlistCommand, WishlistProductResponse>
    {
        private readonly ILogger<MoveFromBagToWishlistCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public MoveFromBagToWishlistCommandHandler(
            ILogger<MoveFromBagToWishlistCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<WishlistProductResponse> Handle(MoveFromBagToWishlistCommand request, CancellationToken cancellationToken)
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

                // Find bag item
                var bagItem = await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                if (bagItem == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found in bag");
                }

                // Check if already in wishlist
                var existingWishlistItem = await _dbContext.UserWishlistProducts
                    .FirstOrDefaultAsync(w => w.UserId == user.Id 
                        && w.WishlistProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(w.ProductVariantCombinationUid)
                            : w.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                ProductVariantCombination variantCombination = null;
                if (!string.IsNullOrEmpty(request.ProductVariantCombinationUid))
                {
                    variantCombination = await _dbContext.ProductVariantCombinations
                        .Include(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                        .FirstOrDefaultAsync(pvc => pvc.Uid == request.ProductVariantCombinationUid 
                            && pvc.ProductId == product.Id, cancellationToken);
                }

                if (existingWishlistItem == null)
                {
                    // Create new wishlist item
                    var wishlistItem = new UserWishlistProduct
                    {
                        WishlistProduct = product,
                        User = user,
                        ProductVariantCombinationUid = request.ProductVariantCombinationUid,
                        ProductVariantCombination = variantCombination
                    };

                    _dbContext.UserWishlistProducts.Add(wishlistItem);
                }

                // Remove from bag
                _dbContext.UserBagProducts.Remove(bagItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Build response
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
                    ProductVariantCombinationUid = request.ProductVariantCombinationUid,
                    Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null),
                    InWishlist = true
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

                if (variantCombination != null)
                {
                    var combinationOptions = variantCombination.CombinationOptions?
                        .Where(co => co?.ProductVariantOption != null)
                        .ToList() ?? new List<ProductVariantCombinationOption>();

                    response.SelectedProductVariantCombination = new ProductVariantCombinationResponse
                    {
                        Uid = variantCombination.Uid,
                        SKU = variantCombination.SKU,
                        Price = variantCombination.Price,
                        Quantity = variantCombination.Quantity,
                        ImageUrl = variantCombination.ImageUrl,
                        IsAvailable = variantCombination.IsAvailable,
                        DisplayName = string.Join(", ",
                            combinationOptions.Select(co => co.ProductVariantOption.Value)),
                        VariantValues = combinationOptions
                            .OrderBy(co => co.ProductVariantOption.ProductVariant?.Id ?? 0)
                            .Select(co => co.ProductVariantOption.Value)
                            .ToArray()
                    };
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error moving product from bag to wishlist: {Message}", e.Message);
                throw;
            }
        }
    }
}
