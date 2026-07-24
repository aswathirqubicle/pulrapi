namespace Core.Application.Models.Stripe;

/// <summary>
/// A single item being exchanged. The payment API uses these identifiers to
/// look up the originally paid price (from the order snapshot) and the new
/// combination's current price, then charges only the positive difference.
/// The client sends identifiers only — never prices or the difference.
/// </summary>
public class ExchangeItemRequest
{
    /// <summary>
    /// The original purchased line item — <c>OrderProductAffiliate.Uid</c> (the sub-order UID).
    /// Source of the originally paid price snapshot.
    /// </summary>
    public string ProductOrderUid { get; set; } = string.Empty;

    /// <summary>
    /// Optional new product UID. When provided, used to sanity-check that the new
    /// combination belongs to this product.
    /// </summary>
    public string? NewProductUid { get; set; }

    /// <summary>
    /// The new variant combination the user is exchanging into —
    /// <c>ProductVariantCombination.Uid</c>. Source of the new (current) price.
    /// </summary>
    public string NewVariantCombinationUid { get; set; } = string.Empty;

    /// <summary>
    /// Exchanged quantity. Clamped server-side to the original purchased quantity.
    /// </summary>
    public int Quantity { get; set; } = 1;
}
