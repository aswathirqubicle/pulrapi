
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProductSimilar : EntityBase
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int SimilarId { get; set; }
        public Product Similar { get; set; }
    }
}
