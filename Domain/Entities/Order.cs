using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Order : EntityBase
{
    [Required]
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    [Required]
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
    [Required]
    public int ShippingDetailsId { get; set; }
    public ShippingDetails ShippingDetails { get; set; }
    
    /// <summary>
    /// Optional billing address. If null, ShippingDetails is used for billing.
    /// </summary>
    public int? BillingDetailsId { get; set; }
    public ShippingDetails BillingDetails { get; set; }
    
    [Required]
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public OrderStatusEnum OrderStatus { get; set; }
    public string RawRequest { get; set; } // minus card data
    
    /// <summary>
    /// Total order amount in major currency units (e.g. 40.00 means $40.00).
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Optional note/instructions from the customer for this order.
    /// </summary>
    public string? Note { get; set; }
    
    /// <summary>
    /// Stripe PaymentMethod ID used for this order (if paid via Stripe).
    /// </summary>
    public string? StripePaymentMethodId { get; set; }
    
    /// <summary>
    /// Tracking number provided by the seller when marking order as shipped.
    /// </summary>
    public string? TrackingNumber { get; set; }
    
    /// <summary>
    /// Shipping provider/carrier (e.g., "FedEx", "UPS", "DHL", "Aramex").
    /// </summary>
    public string? ShippingProvider { get; set; }
    
    /// <summary>
    /// Timestamp when the seller marked the order as shipped.
    /// </summary>
    public DateTime? ShippedAt { get; set; }
    
    /// <summary>
    /// Timestamp when the buyer confirmed delivery.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
    
    public virtual ICollection<OrderProductAffiliate> OrderProductAffiliates { get; set; }
}
