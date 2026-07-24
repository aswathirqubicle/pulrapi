using System;
using System.Collections.Generic;
using Core.Application.Mediatr.Categories.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;

namespace Core.Application.Models.Products;

public class ProductDetailsResponse
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public string WhatIsIt { get; set; }
    public string ProductDetail { get; set; }
    public string Brand { get; set; }
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
    public string CountryCode { get; set; }
    public string CurrencyCode { get; set; }
    public string ProductUrl { get; set; }
    public ProductTypeEnum Type { get; set; }
    public ProductSellTypeEnum SellType { get; set; }
    public List<MediaFileDetailsResponse> ProductMediaFiles { get; set; } = [];
    public List<ProductVariantResponse> ProductVariants { get; set; } = [];
    public Dictionary<string, List<ProductVariantCombinationResponse>> ProductVariantCombinations { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ProfileBaseResponse Profile { get; set; }
    public bool InWishlist { get; set; }
    public bool IsDeletable { get; set; } = true;
    public string? CollabId { get; set; }
}