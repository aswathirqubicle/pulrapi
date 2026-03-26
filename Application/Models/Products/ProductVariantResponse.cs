using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class ProductVariantResponse
    {
        public string VariantName { get; set; }
        public List<string> VariantOptions { get; set; } = [];
    }
}