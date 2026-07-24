using System;
using System.Collections.Generic;
using Core.Application.Models.BagItems;
using Core.Domain.Enums;

namespace Core.Application.Models.Orders
{
    public class OrderProductResponseDto
    {
        public int OrderItemId { get; set; } // ID of the OrderProductAffiliate
        public string OrderUid { get; set; }
        public string ProductOrderUid { get; set; }
        public OrderProductDetailsResponse Product { get; set; }
        public string OrderType { get; set; } // "Purchase" or "Sale"
        public DateTime DeliveryWithin { get; set; } // Expected delivery date
        public DateTime PlacementDate { get; set; } // Order placement date
        public DateTime? TransactionDate { get; set; } // When payment/refund occurred for this item

        // Item-level status tracking
        public OrderStatusEnum OrderItemStatus { get; set; }
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public List<string> ShippingProofImageUrls { get; set; }

        // Indicates if item was shipped before failing (for OrderFailed items)
        public bool WasShipped { get; set; }

        // Retry/Reorder tracking
        public int RetryCount { get; set; }
        public DateTime? CountdownExpiryDate { get; set; }
        public DateTime? NewCountdownExpiryDate { get; set; }
        public bool IsRetryAllowed { get; set; }
        public bool IsCountdownExpired { get; set; }

        // Delivery extension tracking (for shipped items)
        public int ExtensionCount { get; set; }
        public DateTime? ExtensionExpiryDate { get; set; }
        public bool IsExtensionExpired { get; set; }

        // Countdown display label for UI
        public string CountdownLabel { get; set; }

        // Action flags for buyer
        public bool CanRefund { get; set; }
        public bool CanReorder { get; set; }

        // Action flags for shipped items with expired countdown
        public bool CanExtend { get; set; }  // Buyer can extend delivery countdown (only once)
        public bool CanReportIssue { get; set; }  // When extension expired, buyer can report issue
    }
}

