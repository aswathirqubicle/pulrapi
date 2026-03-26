using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class UserWishlistProduct : EntityBase
    {
        [Required]
        public int WishlistProductId { get; set; }
        public Product WishlistProduct { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        
        // Store the variant combination UID to preserve size/color selection
        public string ProductVariantCombinationUid { get; set; }
        public ProductVariantCombination ProductVariantCombination { get; set; }
    }
}

