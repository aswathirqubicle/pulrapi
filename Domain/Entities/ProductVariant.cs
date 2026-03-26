using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductVariant : EntityBase
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string VariantName { get; set; }

        public virtual ICollection<ProductVariantOption> ProductVariantOptions { get; set; } = [];
    }
}
