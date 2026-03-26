using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductAttribute : EntityBase
    {
        public Product Product { get; set; }

        // COLOR
        public string Key { get; set; }
        // RED | GREEN | BLUe

        public ICollection<ProductVariantOption> ProductVariantOptions { get; set; }
    }
}
