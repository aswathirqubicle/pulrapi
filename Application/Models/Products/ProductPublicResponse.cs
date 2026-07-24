using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;
using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class ProductPublicResponse
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string WhatIsIt { get; set; }
        public string ProductDetail { get; set; }
        public string Brand { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string ProductUrl { get; set; }
        public string StoreName { get; set; }
        public double? Price { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public ProductTypeEnum Type { get; set; }
        public ProductSellTypeEnum SellType { get; set; }
        public List<MediaFileDetailsResponse> ProductMediaFiles { get; set; } = [];
        public List<ProductVariantResponse> ProductVariants { get; set; } = [];
        public Dictionary<string, List<ProductVariantCombinationResponse>> ProductVariantCombinations { get; set; } = [];
        public ProfileBaseResponse Profile { get; set; }
        public bool IsDeletable { get; set; } = true;
        public string? CollabId { get; set; }
    }
}
