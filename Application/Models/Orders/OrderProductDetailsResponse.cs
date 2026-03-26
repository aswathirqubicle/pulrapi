using System.Collections.Generic;
using System.Text.Json.Serialization;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;

namespace Core.Application.Models.Orders
{
    public class OrderProductDetailsResponse
    {
        public int BagQuantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? ShippingCost { get; set; }
        public string DeliveryTime { get; set; }
        public string ImageUrl { get; set; }
        [JsonPropertyName("productuid")]
        public string ProductUid { get; set; }
        [JsonPropertyName("productVariantCombinationUid")]
        public string? ProductVariantCombinationUid { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public ProductTypeEnum Type { get; set; }
        public List<MediaFileDetailsResponse> ProductMediaFiles { get; set; } = new List<MediaFileDetailsResponse>();
        public ProfileBaseResponse Profile { get; set; }
        public List<string> VarinatTypes { get; set; } = new List<string>();
    }
}

