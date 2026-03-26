namespace Core.Application.Models.Stripe;

public class CreateCustomerSessionResponse
{
    public string ClientSecret { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerSessionSecret { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;
}

