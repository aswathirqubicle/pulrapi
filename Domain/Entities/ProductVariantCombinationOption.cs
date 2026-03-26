using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    /// <summary>
    /// Junction table linking ProductVariantCombination to ProductVariantOptions
    /// This allows us to track which specific option values make up each combination
    /// Example: A combination might link to ProductVariantOption "Small", "Blue", and "Wood"
    /// </summary>
    public class ProductVariantCombinationOption : EntityBase
    {
        [Required]
        public int ProductVariantCombinationId { get; set; }
        public ProductVariantCombination ProductVariantCombination { get; set; }

        [Required]
        public int ProductVariantOptionId { get; set; }
        public ProductVariantOption ProductVariantOption { get; set; }
    }
}
