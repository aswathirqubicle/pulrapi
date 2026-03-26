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
using Core.Application.Models.BagItems;
using Core.Application.Models.Products;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.BagItems.Commands
{
    public class AddToBagCommand : IRequest<BagProductResponse>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    public class AddToBagCommandHandler : IRequestHandler<AddToBagCommand, BagProductResponse>
    {
        private readonly ILogger<AddToBagCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public AddToBagCommandHandler(
            ILogger<AddToBagCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<BagProductResponse> Handle(AddToBagCommand request, CancellationToken cancellationToken)
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

                // Validate variant combination if provided
                ProductVariantCombination variantCombination = null;
                if (!string.IsNullOrEmpty(request.ProductVariantCombinationUid))
                {
                    variantCombination = await _dbContext.ProductVariantCombinations
                        .Include(pvc => pvc.CombinationOptions)
                            .ThenInclude(cvo => cvo.ProductVariantOption)
                        .FirstOrDefaultAsync(pvc => pvc.Uid == request.ProductVariantCombinationUid 
                            && pvc.ProductId == product.Id, cancellationToken);

                    if (variantCombination == null)
                    {
                        throw new Core.Application.Exceptions.NotFoundException("Product variant combination not found");
                    }

                    if (!variantCombination.IsAvailable)
                    {
                        throw new Core.Application.Exceptions.BadRequestException("Variant is not available");
                    }
                }

                // Check if already in bag
                var existingBagItem = await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                var existingQuantity = existingBagItem?.Quantity ?? 0;
                var requestedTotalQuantity = existingQuantity + request.Quantity;

                if (variantCombination != null)
                {
                    var remainingStock = variantCombination.Quantity - existingQuantity;
                    if (variantCombination.Quantity < requestedTotalQuantity || remainingStock <= 0)
                    {
                        var availableToAdd = Math.Max(remainingStock, 0);
                        var message = availableToAdd <= 0
                            ? "Requested quantity exceeds available stock. No additional units can be added."
                            : $"Requested quantity exceeds available stock. Only {availableToAdd} more can be added.";

                        throw new Core.Application.Exceptions.BadRequestException(message);
                    }
                }

                if (existingBagItem != null)
                {
                    // Increment quantity if already exists
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

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get the updated bag item for response
                var bagItemForResponse = existingBagItem ?? await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                // Check if product is in wishlist
                bool isInWishlist = await _dbContext.UserWishlistProducts
                    .AnyAsync(w => w.UserId == user.Id && w.WishlistProductId == product.Id, cancellationToken);

                // Build response
                var response = BuildBagProductResponse(product, request.ProductVariantCombinationUid, variantCombination, bagItemForResponse.Quantity);
                response.InWishlist = isInWishlist;
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error adding/removing product to/from bag: {Message}", e.Message);
                throw;
            }
        }

        private BagProductResponse BuildBagProductResponse(
            Product product,
            string productVariantCombinationUid,
            ProductVariantCombination variantCombination,
            int quantity)
        {
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
                BagQuantity = quantity,
                ProductVariantCombinationUid = productVariantCombinationUid,
                Price = variantCombination?.Price ?? (product.MinPrice != null ? (decimal?)product.MinPrice : null)
            };

            // Extract size and color from variant combination
            if (variantCombination?.CombinationOptions != null && variantCombination.CombinationOptions.Any())
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
    }
}

