using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.BagItems
{
    public class BagProductDto
    {
        [Required]
        public string Uid { get; set; }
        
        // Optional: Variant combination UID for size/color selection
        public string ProductVariantCombinationUid { get; set; }
        
        public int BagQuantity { get; set; }
    }
}
