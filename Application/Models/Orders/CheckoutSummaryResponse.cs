using System.Collections.Generic;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Models.Orders;

public class CheckoutSummaryResponse
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public decimal TotalShippingCost { get; set; }
    public decimal TotalProductCost { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal? StripeProcessingFee { get; set; }
    public decimal NetOrderAmount { get; set; }
    public List<CheckoutProductSummary> Products { get; set; } = new();

    public CheckoutPaymentResponse? Payment { get; set; }

    public ShippingDetailsResponse? ShippingDetails { get; set; }

    public ShippingDetailsResponse? BillingDetails { get; set; }

    public string? Note { get; set; }

    public string? DeliveryTime { get; set; }
}

public class CheckoutPaymentResponse
{
    public string Brand { get; set; } = string.Empty; // Visa || Master
    public string PaymentMethod { get; set; } = string.Empty; // "Cash on delivery" || "Card"
}
