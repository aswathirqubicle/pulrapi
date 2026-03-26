using System.Collections.Generic;
using Core.Application.Models.Currencies;

namespace Core.Application.Models.Wishlist
{
    public class WishlistResponse
    {
        public List<WishlistProductResponse> Products { get; set; } = new List<WishlistProductResponse>();
        public CurrencyDetailsResponse Currency { get; set; }
        public int TotalCount { get; set; }
    }
}

