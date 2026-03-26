using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductClick : EntityBase
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        
        public int Count { get; set; } = 1;
    }
}

