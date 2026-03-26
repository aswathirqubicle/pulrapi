using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Core.Domain.Entities
{
    /// <summary>
    /// Represents a unique product variant combination with its own SKU, price, and inventory
    /// This stores each unique combination generated from ProductVariants (e.g., Small-Blue-Wood)
    /// Links to ProductVariantOptions to define which specific values make up this combination
    /// </summary>
    public class ProductVariantCombination : EntityBase
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } // Unique SKU for this specific variant combination

        public decimal? Price { get; set; } // Price for this specific variant (can override base product price)

        public int Quantity { get; set; } = 0; // Inventory quantity for this variant

        [MaxLength(500)]
        public string ImageUrl { get; set; } // Optional: specific image for this variant

        public bool IsAvailable { get; set; } = true; // Whether this variant is available for purchase

        // Links to the specific ProductVariantOption values that make up this combination
        // For example: [Small (from Size variant), Blue (from Color variant), Wood (from Material variant)]
        public virtual ICollection<ProductVariantCombinationOption> CombinationOptions { get; set; } = new List<ProductVariantCombinationOption>();
    }
}
