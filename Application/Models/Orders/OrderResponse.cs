using System;
using System.Collections.Generic;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Models.Orders
{
    public class OrderResponse
    {
        public string Status { get; set; }
        public string Uid { get; set; }
        public string ProfileUid { get; set; }
        public OrderPaymentResponse Payment { get; set; }
        public ShippingDetailsResponse ShippingDetails { get; set; }
        public ShippingDetailsResponse BillingDetails { get; set; }
        public decimal Amount { get; set; }
        public string BuyerFullName { get; set; }
        public List<string> SellerFullNames { get; set; }
        public bool IsProcessing { get; set; }
        public ICollection<OrderProductResponseDto> OrderProducts { get; set; }
        
        // Shipping tracking information
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime PlacementDate { get; set; }
        public DateTime? TransactionDate { get; set; }  // When payment was made


    }
}
