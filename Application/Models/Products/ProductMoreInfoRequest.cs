using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    public class ProductMoreInfoRequest
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Info { get; set; }
    }
}
