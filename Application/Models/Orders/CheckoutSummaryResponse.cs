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
    public List<CheckoutProductSummary> Products { get; set; } = new();

    /// <summary>
    /// Simplified payment info (no sensitive card details).
    /// </summary>
    public CheckoutPaymentResponse? Payment { get; set; }

    /// <summary>
    /// Simplified shipping details for checkout summary.
    /// </summary>
    public ShippingDetailsResponse? ShippingDetails { get; set; }

    /// <summary>
    /// Billing address for the order. If null, ShippingDetails is used for billing.
    /// </summary>
    public ShippingDetailsResponse? BillingDetails { get; set; }

    /// <summary>
    /// Optional note/instructions sent with the payment request.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Overall estimated delivery time for the order.
    /// </summary>
    public string? DeliveryTime { get; set; }
}

public class CheckoutPaymentResponse
{
    public string Brand { get; set; } = string.Empty; // Visa || Master
    public string PaymentMethod { get; set; } = string.Empty; // "Cash on delivery" || "Card"
}
