using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Wishlist
{
    public class WishlistProductDto
    {
        [Required]
        public string ProductUid { get; set; }
        
        // Optional: Variant combination UID for size/color selection
        public string ProductVariantCombinationUid { get; set; }
    }
}

