using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class FeaturedProductsResponse
    {
        public PagingResponse<ProductPublicResponse> HotSellerProducts { get; set; }
        public PagingResponse<ProductPublicResponse> NewInProducts { get; set; }
    }
}
