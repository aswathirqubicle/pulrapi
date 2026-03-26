using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductVariantOption : EntityBase
    {
        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        [Required]
        [MaxLength(100)]
        public string Value { get; set; }
    }
}
