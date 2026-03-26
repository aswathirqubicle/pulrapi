using System;
using System.Collections.Generic;
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
    /// Request model for updating a single variant combination
    /// </summary>
    public class VariantCombinationUpdateRequest
    {
        [Required(ErrorMessage = "Combination UID is required")]
        [SafeUid(allowNullValue: false, ErrorMessage = "Combination UID contains invalid characters or format")]
        public string CombinationUid { get; set; }

        [MaxLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string SKU { get; set; }

        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int? Quantity { get; set; }

        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string ImageUrl { get; set; }

        public bool? IsAvailable { get; set; }
    }

    /// <summary>
    /// Command to update multiple product variant combinations in one request
    /// Only updates the fields that are provided for each combination
    /// </summary>
    public class ProductVariantCombinationsBulkUpdateCommand : IRequest<List<ProductVariantCombinationResponse>>
    {
        [Required(ErrorMessage = "Product UID is required")]
        [SafeUid(allowNullValue: false, ErrorMessage = "Product UID contains invalid characters or format")]
        public string ProductUid { get; set; }

        [Required(ErrorMessage = "At least one combination update is required")]
        [MinLength(1, ErrorMessage = "At least one combination update is required")]
        public List<VariantCombinationUpdateRequest> CombinationUpdates { get; set; } = new List<VariantCombinationUpdateRequest>();
    }

    public class ProductVariantCombinationsBulkUpdateCommandHandler : IRequestHandler<ProductVariantCombinationsBulkUpdateCommand, List<ProductVariantCombinationResponse>>
    {
        private readonly ILogger<ProductVariantCombinationsBulkUpdateCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ProductVariantCombinationsBulkUpdateCommandHandler(
            ILogger<ProductVariantCombinationsBulkUpdateCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<ProductVariantCombinationResponse>> Handle(ProductVariantCombinationsBulkUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync() ?? throw new UnauthorizedAccessException("User not found or not logged in");

                // Verify product exists and belongs to user
                var product = await _dbContext.Products
                    .SingleOrDefaultAsync(p => p.Uid == request.ProductUid && 
                                             p.UserId == user.Id && 
                                             p.IsActive, cancellationToken);

                if (product == null)
                {
                    throw new NotFoundException("Product not found");
                }

                // Get all combination UIDs to update
                var combinationUids = request.CombinationUpdates.Select(cu => cu.CombinationUid).ToList();

                // Find all combinations to update
                var combinations = await _dbContext.ProductVariantCombinations
                    .Include(pvc => pvc.CombinationOptions)
                        .ThenInclude(co => co.ProductVariantOption)
                            .ThenInclude(pvo => pvo.ProductVariant)
                    .Where(pvc => combinationUids.Contains(pvc.Uid) && 
                                 pvc.ProductId == product.Id && 
                                 pvc.IsActive)
                    .ToListAsync(cancellationToken);

                if (combinations.Count != request.CombinationUpdates.Count)
                {
                    throw new BadRequestException("Some variant combinations were not found");
                }

                // Check for SKU uniqueness across all updates
                var skuUpdates = request.CombinationUpdates
                    .Where(cu => !string.IsNullOrWhiteSpace(cu.SKU))
                    .ToList();

                if (skuUpdates.Any())
                {
                    var newSkus = skuUpdates.Select(su => su.SKU).ToList();
                    var existingSkus = await _dbContext.ProductVariantCombinations
                        .Where(pvc => newSkus.Contains(pvc.SKU) && 
                                     !combinationUids.Contains(pvc.Uid))
                        .Select(pvc => pvc.SKU)
                        .ToListAsync(cancellationToken);

                    if (existingSkus.Any())
                    {
                        throw new BadRequestException($"SKUs already exist: {string.Join(", ", existingSkus)}");
                    }

                    // Check for duplicate SKUs within the update request
                    var duplicateSkus = newSkus.GroupBy(sku => sku)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateSkus.Any())
                    {
                        throw new BadRequestException($"Duplicate SKUs in request: {string.Join(", ", duplicateSkus)}");
                    }
                }

                var updatedCombinations = new List<ProductVariantCombinationResponse>();

                // Update each combination
                foreach (var updateRequest in request.CombinationUpdates)
                {
                    var combination = combinations.Single(c => c.Uid == updateRequest.CombinationUid);
                    bool hasChanges = false;

                    // Update only provided fields
                    if (!string.IsNullOrWhiteSpace(updateRequest.SKU))
                    {
                        combination.SKU = updateRequest.SKU;
                        hasChanges = true;
                    }

                    if (updateRequest.Price.HasValue)
                    {
                        combination.Price = updateRequest.Price.Value;
                        hasChanges = true;
                    }

                    if (updateRequest.Quantity.HasValue)
                    {
                        combination.Quantity = updateRequest.Quantity.Value;
                        hasChanges = true;
                    }

                    if (updateRequest.ImageUrl != null) // Allow empty string to clear image
                    {
                        combination.ImageUrl = updateRequest.ImageUrl;
                        hasChanges = true;
                    }

                    if (updateRequest.IsAvailable.HasValue)
                    {
                        combination.IsAvailable = updateRequest.IsAvailable.Value;
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        combination.UpdatedAt = DateTime.UtcNow;
                        combination.LastUpdatedBy = user.Id;
                    }

                    // Prepare response
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

                    updatedCombinations.Add(response);
                }

                // Save all changes
                _dbContext.ProductVariantCombinations.UpdateRange(combinations);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return updatedCombinations;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
