using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    /// <summary>
    /// Request model for updating or creating a product variant combination
    /// If Uid is provided, it updates an existing combination. If not provided, it creates a new one.
    /// </summary>
    public class ProductVariantCombinationUpdateRequest
    {
        // Optional: If provided, updates existing combination. If not provided, creates new one.
        public string Uid { get; set; }

        [Required(ErrorMessage = "SKU is required")]
        [MaxLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        public string SKU { get; set; }

        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int? Quantity { get; set; }

        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string ImageUrl { get; set; }

        public bool? IsAvailable { get; set; }

        // The combination values (e.g., ["Small", "Blue", "Wood"])
        // Required when creating new combinations
        public string[] VariantValues { get; set; }
    }
}
