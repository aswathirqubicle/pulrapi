using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Wishlist;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Wishlist.Commands
{
    public class AddToWishlistCommand : IRequest<WishlistProductResponse>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
    }

    public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, WishlistProductResponse>
    {
        private readonly ILogger<AddToWishlistCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public AddToWishlistCommandHandler(
            ILogger<AddToWishlistCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<WishlistProductResponse> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(true);
                if (user == null)
                {
                    throw new Core.Application.Exceptions.NotAuthenticatedException("User not authenticated");
                }

                // Check if product exists
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

                // Check if already in wishlist (TOGGLE LOGIC)
                var existingWishlistItem = await _dbContext.UserWishlistProducts
                    .FirstOrDefaultAsync(w => w.UserId == user.Id 
                        && w.WishlistProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(w.ProductVariantCombinationUid)
                            : w.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                if (existingWishlistItem != null)
                {
                    // TOGGLE: Remove from wishlist if already exists
                    _dbContext.UserWishlistProducts.Remove(existingWishlistItem);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Return response with InWishlist = false
                    var removeResponse = BuildWishlistProductResponse(product, request.ProductVariantCombinationUid, null);
                    removeResponse.InWishlist = false;
                    return removeResponse;
                }

                // Validate variant combination if provided
                ProductVariantCombination variantCombination = null;
                if (!string.IsNullOrEmpty(request.ProductVariantCombinationUid))
                {
                    variantCombination = await _dbContext.ProductVariantCombinations
                        .Include(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                        .FirstOrDefaultAsync(pvc => pvc.Uid == request.ProductVariantCombinationUid 
                            && pvc.ProductId == product.Id, cancellationToken);

                    if (variantCombination == null)
                    {
                        throw new Core.Application.Exceptions.NotFoundException("Product variant combination not found");
                    }
                }


                // Create wishlist item
                var wishlistItem = new UserWishlistProduct
                {
                    WishlistProduct = product,
                    User = user,
                    ProductVariantCombinationUid = request.ProductVariantCombinationUid,
                    ProductVariantCombination = variantCombination
                };

                _dbContext.UserWishlistProducts.Add(wishlistItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Build response with InWishlist = true
                var response = BuildWishlistProductResponse(product, request.ProductVariantCombinationUid, variantCombination);
                response.InWishlist = true;
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error adding/removing product to/from wishlist: {Message}", e.Message);
                throw;
            }
        }

        private WishlistProductResponse BuildWishlistProductResponse(
            Product product, 
            string productVariantCombinationUid, 
            ProductVariantCombination variantCombination)
        {
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
                ProductVariantCombinationUid = productVariantCombinationUid,
                Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null)
            };

            // Extract size and color from variant combination
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

            return response;
        }
    }
}

