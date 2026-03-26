namespace Core.Application.Models.Orders;

public class CheckoutProductRequest
{
    public string ProductUid { get; set; } = string.Empty;
    public string? VariantCombinationUid { get; set; }
    public int Quantity { get; set; } = 1;
}

