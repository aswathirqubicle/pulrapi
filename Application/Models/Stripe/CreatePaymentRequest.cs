using System.Collections.Generic;
using Core.Application.Models.Orders;

namespace Core.Application.Models.Stripe;

public class CreatePaymentRequest
{
    /// <summary>
    /// Amount in major currency units (e.g. 10.50 means $10.50).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO currency code (e.g. "usd").
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Optional saved Stripe PaymentMethod ID.
    /// If provided, the backend will immediately charge this saved card
    /// using an off-session PaymentIntent.
    /// If null/empty, the API behaves like before and returns client secrets
    /// for the mobile PaymentSheet flow.
    /// </summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Optional free-text note or instructions for this order/payment.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// List of products in the order (required for checkout summary).
    /// </summary>
    public List<CheckoutProductRequest> Products { get; set; } = new();

    /// <summary>
    /// Optional shipping details UID. If not provided, uses default shipping address.
    /// </summary>
    public string? ShippingDetailsUid { get; set; }
    
    /// <summary>
    /// Optional billing address UID. If not provided, uses shipping address for billing.
    /// </summary>
    public string? BillingAddressDetailsUid { get; set; }

    /// <summary>
    /// Optional internal Order ID (e.g. P001) to link with Stripe metadata.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Required for 3D Secure redirect flows when Confirm = true.
    /// Example: "pulr://payment-complete" or "https://app.pulr.co/payment-complete"
    /// </summary>
    public string? ReturnUrl { get; set; }
}
