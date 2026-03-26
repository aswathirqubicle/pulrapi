using System.Collections.Generic;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;

namespace Core.Application.Models.Orders;

public class CheckoutProductSummary
{
    public string Uid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public ProductTypeEnum Type { get; set; }
    public List<MediaFileDetailsResponse> ProductMediaFiles { get; set; } = new();
    public List<ProductVariantResponse> ProductVariants { get; set; } = new();
    public int BagQuantity { get; set; }
    public string? ProductVariantCombinationUid { get; set; }
    public decimal Price { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? DeliveryTime { get; set; }
    public ProductVariantCombinationResponse? ProductVariantCombinations { get; set; }
    
    /// <summary>
    /// Individual product order ID (e.g., P005-01, P005-02). 
    /// Only populated after order is saved to database.
    /// </summary>
    public string? ProductOrderUid { get; set; }
    
    /// <summary>
    /// Product owner's profile information
    /// </summary>
    public ProfileResponse? OwnerProfile { get; set; }
}


