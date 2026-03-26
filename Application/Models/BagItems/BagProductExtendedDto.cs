using System.Collections.Generic;
using System.Text.Json.Serialization;
using Core.Application.Models.Products;

namespace Core.Application.Models.BagItems
{
    public class BagProductExtendedDto : ProductDetailsResponse
    {
        public int BagQuantity { get; set; }
        public string ProductVariantCombinationUid { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public decimal? Price { get; set; }
        public decimal? ShippingCost { get; set; }
        public string DeliveryTime { get; set; }
        [JsonIgnore]
        public new Dictionary<string, List<ProductVariantCombinationResponse>> ProductVariantCombinations
        {
            get => base.ProductVariantCombinations;
            set => base.ProductVariantCombinations = value;
        }
        [JsonPropertyName("productVariantCombinations")]
        public ProductVariantCombinationResponse SelectedProductVariantCombination { get; set; }
        public string ImageUrl { get; internal set; }
        public new bool InWishlist { get; set; }
    }
}
