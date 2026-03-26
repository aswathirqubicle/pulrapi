namespace Core.Application.Models.Stripe;

public class SetupIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerSessionSecret { get; set; } = string.Empty;
    public string CustomerEphemeralKeySecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}
