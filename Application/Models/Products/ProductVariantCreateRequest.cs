using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    public class ProductVariantCreateRequest
    {
        public string VariantName { get; set; }
        [MinLength(1, ErrorMessage = "At least one option is required")]
        public List<string> VariantOptions { get; set; } = [];
    }
}
