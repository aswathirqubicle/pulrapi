using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductMoreInfo : EntityBase
    {
        public string Title { get; set; }
        public string Info { get; set; }
        [Required]
        public Product Product { get; set; }
    }
}
