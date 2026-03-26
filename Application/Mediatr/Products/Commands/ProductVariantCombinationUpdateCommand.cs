using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Application.Security.Validation.Attributes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Commands
{
    /// <summary>
    /// Command to update a specific product variant combination
    /// Only updates the fields that are provided (partial update)
    /// </summary>
    public class ProductVariantCombinationUpdateCommand : IRequest<ProductVariantCombinationResponse>
    {
        [Required(ErrorMessage = "Combination UID is required")]
        [SafeUid(allowNullValue: false, ErrorMessage = "Combination UID contains invalid characters or format")]
        public string CombinationUid { get; set; }

        // Optional fields - only update if provided
        [MaxLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string SKU { get; set; }

        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int? Quantity { get; set; }

        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string ImageUrl { get; set; }

        public bool? IsAvailable { get; set; }
    }

    public class ProductVariantCombinationUpdateCommandHandler : IRequestHandler<ProductVariantCombinationUpdateCommand, ProductVariantCombinationResponse>
    {
        private readonly ILogger<ProductVariantCombinationUpdateCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ProductVariantCombinationUpdateCommandHandler(
            ILogger<ProductVariantCombinationUpdateCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<ProductVariantCombinationResponse> Handle(ProductVariantCombinationUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync() ?? throw new UnauthorizedAccessException("User not found or not logged in");

                // Find the variant combination and ensure it belongs to the user
                var combination = await _dbContext.ProductVariantCombinations
                    .Include(pvc => pvc.Product)
                    .Include(pvc => pvc.CombinationOptions)
                        .ThenInclude(co => co.ProductVariantOption)
                            .ThenInclude(pvo => pvo.ProductVariant)
                    .SingleOrDefaultAsync(pvc => pvc.Uid == request.CombinationUid && 
                                                pvc.Product.UserId == user.Id && 
                                                pvc.IsActive, cancellationToken);

                if (combination == null)
                {
                    throw new NotFoundException("Product variant combination not found");
                }

                // Update only the fields that are provided (partial update)
                bool hasChanges = false;

                if (!string.IsNullOrWhiteSpace(request.SKU))
                {
                    // Check if SKU is unique (excluding current combination)
                    var existingSku = await _dbContext.ProductVariantCombinations
                        .AnyAsync(pvc => pvc.SKU == request.SKU && pvc.Id != combination.Id, cancellationToken);

                    if (existingSku)
                    {
                        throw new BadRequestException($"SKU '{request.SKU}' already exists");
                    }

                    combination.SKU = request.SKU;
                    hasChanges = true;
                }

                if (request.Price.HasValue)
                {
                    combination.Price = request.Price.Value;
                    hasChanges = true;
                }

                if (request.Quantity.HasValue)
                {
                    combination.Quantity = request.Quantity.Value;
                    hasChanges = true;
                }

                if (request.ImageUrl != null) // Allow empty string to clear image
                {
                    combination.ImageUrl = request.ImageUrl;
                    hasChanges = true;
                }

                if (request.IsAvailable.HasValue)
                {
                    combination.IsAvailable = request.IsAvailable.Value;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    combination.UpdatedAt = DateTime.UtcNow;
                    combination.LastUpdatedBy = user.Id;
                    
                    _dbContext.ProductVariantCombinations.Update(combination);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // Return the updated combination
                var response = new ProductVariantCombinationResponse
                {
                    Uid = combination.Uid,
                    SKU = combination.SKU,
                    Price = combination.Price,
                    Quantity = combination.Quantity,
                    ImageUrl = combination.ImageUrl,
                    IsAvailable = combination.IsAvailable,
                    DisplayName = string.Join(", ", combination.CombinationOptions
                        .Select(co => co.ProductVariantOption.Value)),
                    VariantValues = combination.CombinationOptions
                        .OrderBy(co => co.ProductVariantOption.ProductVariant.Id)
                        .Select(co => co.ProductVariantOption.Value)
                        .ToArray()
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
