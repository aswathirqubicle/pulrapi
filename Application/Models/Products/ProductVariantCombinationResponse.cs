using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    /// <summary>
    /// Response model for a product variant combination
    /// </summary>
    public class ProductVariantCombinationResponse
    {
        public string Uid { get; set; }
        public string SKU { get; set; }
        public decimal? Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        
        // Array of variant option values (e.g., ["Small", "Blue", "Wood"])
        public string[] VariantValues { get; set; } = [];
        
        // Display name for the combination (e.g., "Small, Blue, Wood")
        public string DisplayName { get; set; }
    }
}
