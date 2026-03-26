using Core.Application.Models.Products;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Core.Application.Models.Wishlist
{
    public class WishlistProductResponse : ProductDetailsResponse
    {
        public string ProductVariantCombinationUid { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public decimal? Price { get; set; }
        [JsonIgnore]
        public new Dictionary<string, List<ProductVariantCombinationResponse>> ProductVariantCombinations
        {
            get => base.ProductVariantCombinations;
            set => base.ProductVariantCombinations = value;
        }
        [JsonPropertyName("productVariantCombinations")]
        public ProductVariantCombinationResponse SelectedProductVariantCombination { get; set; }
        public new bool InWishlist { get; set; } = true;
    }
}

