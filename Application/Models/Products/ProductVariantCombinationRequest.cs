using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    /// <summary>
    /// Request model for a specific product variant combination with SKU, price, and inventory
    /// </summary>
    public class ProductVariantCombinationRequest
    {
        [Required(ErrorMessage = "SKU is required")]
        [MaxLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string SKU { get; set; }

        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; } = 0;

        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        // The combination values (e.g., ["Small", "Blue", "Wood"])
        // These should match the order of variant options provided
        [Required(ErrorMessage = "Variant values are required")]
        public string[] VariantValues { get; set; }
    }
}
