using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProductCategory : EntityBase
    {
        [Required]
        public int CategoryId { get; set; }

        public Category Category { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}