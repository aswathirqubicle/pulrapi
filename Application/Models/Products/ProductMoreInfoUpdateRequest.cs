using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    public class ProductMoreInfoUpdateRequest
    {
        public string Uid { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Info { get; set; }
    }
}
