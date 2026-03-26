namespace Core.Application.Models.Stripe;

public class SaveCardRequest
{
    /// <summary>
    /// Card number (e.g., "4242424242424242").
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Expiration month (1-12).
    /// </summary>
    public int ExpMonth { get; set; }

    /// <summary>
    /// Expiration year (e.g., 2025).
    /// </summary>
    public int ExpYear { get; set; }

    /// <summary>
    /// Card security code (CVC).
    /// </summary>
    public string Cvc { get; set; } = string.Empty;

    /// <summary>
    /// Set this card as the default payment method.
    /// </summary>
    public bool SetAsDefault { get; set; } = false;
}
