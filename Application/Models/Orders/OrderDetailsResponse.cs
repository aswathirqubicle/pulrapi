using System;
using System.Collections.Generic;
using Core.Application.Models.Currencies;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.Stripe;

namespace Core.Application.Models.Orders
{
    public class OrderDetailsResponse
    {
        public string Uid { get; set; }
        public string PaymentMethodUid { get; set; }
        public string ProfileUid { get; set; }
        public ShippingDetailsResponse ShippingDetails { get; set; }
        public ShippingDetailsResponse? BillingDetails { get; set; }
        public CurrencyDetailsResponse Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal? Vat { get; set; }
        public PaymentBreakdownResponse? PaymentBreakdown { get; set; }
        public string BuyerFullName { get; set; }
        public List<string> SellerFullNames { get; set; }
        public string? Note { get; set; }
        public string? StripePaymentMethodId { get; set; }
        public PaymentMethodResponse? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingProvider { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public virtual ICollection<OrderProductResponseDto> OrderProducts { get; set; }
    }
}
