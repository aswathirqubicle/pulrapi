namespace Core.Application.Models.Stripe;

public class SaveCardResponse
{
    public bool Success { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool RequiresAction { get; set; } = false;
    public string? SetupIntentClientSecret { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
