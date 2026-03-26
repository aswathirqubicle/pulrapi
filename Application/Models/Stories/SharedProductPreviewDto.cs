using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class SharedProductPreviewDto
    {
        public string ProductUid { get; set; }
        public string ProductName { get; set; }
        public string OwnerUsername { get; set; }
        public string OwnerFullName { get; set; }
        public string OwnerProfileImageUrl { get; set; }
        public string WhatIsIt { get; set; }
        public string ProductDetail { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public string ProductImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}


