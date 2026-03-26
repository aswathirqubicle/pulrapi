using System;

namespace Core.Application.Models.Orders;

/// <summary>
/// Configuration settings for order-related operations.
/// </summary>
public class OrderSettings
{
    /// <summary>
    /// Number of hours for delivery extension period.
    /// Default: 72 hours (3 days).
    /// For testing: use 0.083 for 5 minutes, 0.5 for 30 minutes.
    /// </summary>
    public double DeliveryExtensionHours { get; set; } = 72;

    /// <summary>
    /// Calculates the extension expiry date from the current UTC time.
    /// </summary>
    public DateTime CalculateExtensionExpiryDate()
    {
        return DateTime.UtcNow.AddHours(DeliveryExtensionHours);
    }
}