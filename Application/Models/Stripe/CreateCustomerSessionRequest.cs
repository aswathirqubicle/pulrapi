namespace Core.Application.Models.Stripe;

public class CreateCustomerSessionRequest
{
    /// <summary>
    /// Existing Stripe customer id if you already have one (optional).
    /// If not provided, a new customer will be created.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Customer email (used when creating a new customer).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Customer full name (used when creating a new customer).
    /// </summary>
    public string? Name { get; set; }
}

