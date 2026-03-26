using Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Application.Services
{
    /// <summary>
    /// Service for handling product variant combinations using Cartesian product logic
    /// </summary>
    public class ProductVariantService
    {
        /// <summary>
        /// Generates all possible combinations from a list of ProductVariants using Cartesian product
        /// Example: Size[Small, Medium, Large] x Color[Blue, Red] x Material[Wood, Steel]
        /// Results in: 3 x 2 x 2 = 12 combinations
        /// </summary>
        public List<List<ProductVariantOption>> GenerateVariantCombinations(List<ProductVariant> variants)
        {
            if (variants == null || !variants.Any())
                return new List<List<ProductVariantOption>>();

            // Get all option lists from each variant
            var optionLists = variants
                .Select(v => v.ProductVariantOptions?.ToList() ?? new List<ProductVariantOption>())
                .Where(list => list.Any())
                .ToList();

            if (!optionLists.Any())
                return new List<List<ProductVariantOption>>();

            // Generate Cartesian product
            return CartesianProduct(optionLists).ToList();
        }

        /// <summary>
        /// Generates a unique SKU for a variant combination
        /// Format: BASE-{VariantValue1}-{VariantValue2}-{VariantValue3}-{ProductId}
        /// Example: PROD-SMALL-BLUE-WOOD-12345
        /// </summary>
        public string GenerateSKU(string baseProductName, List<ProductVariantOption> combination, int productId)
        {
            var baseSku = SanitizeForSKU(baseProductName);
            var variantParts = combination
                .Select(opt => SanitizeForSKU(opt.Value))
                .Where(part => !string.IsNullOrWhiteSpace(part));

            return $"{baseSku}-{string.Join("-", variantParts)}-{productId}".ToUpperInvariant();
        }

        /// <summary>
        /// Generates a display name for a variant combination
        /// Example: "Small, Blue, Wood"
        /// </summary>
        public string GenerateDisplayName(List<ProductVariantOption> combination)
        {
            return string.Join(", ", combination.Select(opt => opt.Value));
        }

        /// <summary>
        /// Creates ProductVariantCombination entities from combinations
        /// </summary>
        public List<ProductVariantCombination> CreateCombinationEntities(
            int productId,
            string baseProductName,
            List<List<ProductVariantOption>> combinations,
            decimal? basePrice = null,
            int defaultQuantity = 0)
        {
            return combinations.Select(combination => new ProductVariantCombination
            {
                ProductId = productId,
                SKU = GenerateSKU(baseProductName, combination, productId),
                Price = basePrice,
                Quantity = defaultQuantity,
                IsAvailable = true,
                CombinationOptions = combination.Select(option => new ProductVariantCombinationOption
                {
                    ProductVariantOptionId = option.Id
                }).ToList()
            }).ToList();
        }

        #region Private Helper Methods

        /// <summary>
        /// Implements Cartesian product algorithm recursively
        /// </summary>
        private IEnumerable<List<ProductVariantOption>> CartesianProduct(List<List<ProductVariantOption>> lists)
        {
            if (!lists.Any())
            {
                yield return new List<ProductVariantOption>();
                yield break;
            }

            var firstList = lists[0];
            var remainingLists = lists.Skip(1).ToList();

            foreach (var item in firstList)
            {
                foreach (var combination in CartesianProduct(remainingLists))
                {
                    var result = new List<ProductVariantOption> { item };
                    result.AddRange(combination);
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Sanitizes a string for use in SKU (removes special characters, replaces spaces)
        /// </summary>
        private string SanitizeForSKU(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove special characters and replace spaces with hyphens
            var sanitized = new string(input
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray())
                .Trim()
                .Replace(" ", "-");

            // Remove consecutive hyphens
            while (sanitized.Contains("--"))
                sanitized = sanitized.Replace("--", "-");

            return sanitized;
        }

        #endregion
    }
}
