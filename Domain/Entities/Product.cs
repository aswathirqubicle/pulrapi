using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class Product : EntityBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string WhatIsIt { get; set; }
        public string ProductDetail { get; set; }
        public string Brand { get; set; } // Store brand as string for now
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string CountryUid { get; set; }
        public Country Country { get; set; }
        public string ProductUrl { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public int? StoreId { get; set; }
        public Store Store { get; set; }
        public ProductTypeEnum Type { get; set; } = ProductTypeEnum.Product; // Default to regular product
        public ProductSellTypeEnum SellType { get; set; } = ProductSellTypeEnum.SellOnPulr;
        public virtual ICollection<ProductMediaFile> ProductMediaFiles { get; set; }
        public virtual ICollection<ProductVariant> ProductVariant { get; set; }
        public virtual ICollection<ProductVariantCombination> ProductVariantCombinations { get; set; }
    }
}