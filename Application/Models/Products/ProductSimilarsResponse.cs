
using System.Collections.Generic;

using Core.Application.Models.Products;

namespace Core.Application.Models.Products
{
    public class ProductSimilarsResponse
    {
        public List<ProductPublicResponse> Similars { get; set; }
        public List<ProductPublicResponse> Pairs { get; set; }
    }
}
