using Core.Domain.Entities;
using Core.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Core.Domain.Entities;

public class OrderProductAffiliate : EntityBase
{
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int ProductQuantity { get; set; }

    public int? AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; }

    public int? ProductVariantCombinationId { get; set; }
    public ProductVariantCombination ProductVariantCombination { get; set; }

    // Item-level status tracking for multi-seller orders
    public OrderStatusEnum OrderItemStatus { get; set; } = OrderStatusEnum.Processing;
    public string TrackingNumber { get; set; }
    public string ShippingProvider { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Shipping proof attachments (multiple images/PDFs per order item)
    public ICollection<OrderItemShippingProof> ShippingProofs { get; set; } = new List<OrderItemShippingProof>();

    // Retry/Reorder tracking
    public int RetryCount { get; set; } = 0;
    public DateTime? CountdownExpiryDate { get; set; }
    public DateTime? NewCountdownExpiryDate { get; set; }
    public bool IsRetryAllowed { get; set; } = true;

    // Delivery extension tracking (for shipped items when buyer hasn't received)
    public int ExtensionCount { get; set; } = 0;
    public DateTime? ExtensionExpiryDate { get; set; }

    // Escrow and refund/exchange tracking
    public EscrowStatusEnum EscrowStatus { get; set; } = EscrowStatusEnum.PendingDelivery;
    public DateTime? EscrowReleaseAt { get; set; }
    public DateTime? RefundEligibleUntil { get; set; }
    public DateTime? ExchangeEligibleUntil { get; set; }
    public string DeliveryProofUrl { get; set; }
    public int? DeliveredBy { get; set; }
    public DateTime? CheckinPromptSentAt { get; set; }
    public Guid? ColabInviteId { get; set; }
    public string CreatorUserId { get; set; }

    // Product snapshot fields - captured at order time to preserve data even if product is deleted
    public string ProductNameSnapshot { get; set; }
    public string ProductDescriptionSnapshot { get; set; } // Combined WhatIsIt + ProductDetail
    public decimal? ProductPriceSnapshot { get; set; }
    public double? ProductMinPriceSnapshot { get; set; }
    public double? ProductMaxPriceSnapshot { get; set; }
    public string ProductBrandSnapshot { get; set; }
    public string PrimaryImageUrlSnapshot { get; set; }
    public string CountryCodeSnapshot { get; set; }
    public string CurrencyCodeSnapshot { get; set; }
    public int ProductTypeSnapshot { get; set; } // Stored as int (enum value)
    public string ProfileUidSnapshot { get; set; }
    public string ProfileUsernameSnapshot { get; set; }
    public decimal? ShippingCostSnapshot { get; set; }
    public string DeliveryTimeSnapshot { get; set; }
    public string VariantTypesSnapshot { get; set; } // JSON array of variant type strings
    public string ProductVariantCombinationUidSnapshot { get; set; }
}
