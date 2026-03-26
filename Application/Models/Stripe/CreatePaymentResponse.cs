using Core.Application.Models.Orders;
using Core.Application.Models.Wallet;

namespace Core.Application.Models.Stripe;

public class CreatePaymentResponse
{
    public string PaymentIntent { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
    public string CustomerSessionClientSecret { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public bool RequiresAction { get; set; } = false;
    public string? Status { get; set; }

    /// <summary>
    /// Indicates if the payment was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if payment failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Complete checkout summary including products, shipping, and payment method details.
    /// Always included regardless of payment success/failure.
    /// </summary>
    public CheckoutSummaryResponse? CheckoutSummary { get; set; }

    /// <summary>
    /// Full order details including item-level statuses for each product.
    /// Includes tracking information and delivery status for each item.
    /// </summary>
    public OrderDetailsResponse? OrderDetails { get; set; }

    /// <summary>
    /// Overall estimated delivery time for the order.
    /// </summary>
    public string? DeliveryTime { get; set; }

    /// <summary>
    /// Total shipping cost for the order.
    /// </summary>
    public decimal TotalShippingCost { get; set; }

    /// <summary>
    /// Total cost of all products in the order.
    /// </summary>
    public decimal TotalProductCost { get; set; }

    /// <summary>
    /// The wallet transaction details for the buyer.
    /// </summary>
    public WalletTransactionResponse? WalletTransaction { get; set; }
}




