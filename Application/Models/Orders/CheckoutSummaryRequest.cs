using System.Collections.Generic;

namespace Core.Application.Models.Orders;

public class CheckoutSummaryRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? PaymentMethodId { get; set; }
    public List<CheckoutProductRequest> Products { get; set; } = new();
}

