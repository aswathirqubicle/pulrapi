using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    public class ProductImageDeleteDto
    {
        [Required]
        public string ProductUid { get; set; }
        [Required]
        public string ImageUid { get; set; }
    }
}
