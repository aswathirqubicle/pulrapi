using System;
using System.Text.RegularExpressions;

namespace Core.Application.Helpers
{
    public static class OrderHelper
    {
        /// <summary>
        /// Calculates the time remaining until delivery based on order date, delivery time, and current date.
        /// Formula: (Order Date + Delivery Days) - Current Date
        /// </summary>
        /// <param name="orderCreatedAt">The date when the order was created</param>
        /// <param name="deliveryTime">Delivery time string (e.g., "7 days", "3-5 days", "2 weeks")</param>
        /// <returns>TimeSpan showing time remaining (days, hours, minutes), or null if delivery time cannot be parsed</returns>
        public static TimeSpan? CalculateDeliveryWithin(DateTime orderCreatedAt, string deliveryTime)
        {
            if (string.IsNullOrWhiteSpace(deliveryTime))
                return null;

            var deliveryDays = ParseDeliveryDays(deliveryTime);
            if (!deliveryDays.HasValue)
                return null;

            var expectedDeliveryDate = orderCreatedAt.AddDays(deliveryDays.Value);
            var timeRemaining = expectedDeliveryDate - DateTime.UtcNow;

            // Return TimeSpan.Zero if delivery is overdue (negative time)
            return timeRemaining < TimeSpan.Zero ? TimeSpan.Zero : timeRemaining;
        }

        /// <summary>
        /// Parses delivery time string to extract number of days.
        /// Supports formats like: "7 days", "3-5 days", "2 weeks", "1 week"
        /// For ranges (e.g., "3-5 days"), uses the maximum value.
        /// </summary>
        private static int? ParseDeliveryDays(string deliveryTime)
        {
            if (string.IsNullOrWhiteSpace(deliveryTime))
                return null;

            deliveryTime = deliveryTime.ToLower().Trim();

            // Handle ranges like "3-5 days" - take the maximum
            var rangeMatch = Regex.Match(deliveryTime, @"(\d+)\s*-\s*(\d+)\s*(day|week)s?");
            if (rangeMatch.Success)
            {
                var maxValue = int.Parse(rangeMatch.Groups[2].Value);
                var unit = rangeMatch.Groups[3].Value;
                return unit == "week" ? maxValue * 7 : maxValue;
            }

            // Handle single values like "7 days" or "2 weeks"
            var singleMatch = Regex.Match(deliveryTime, @"(\d+)\s*(day|week)s?");
            if (singleMatch.Success)
            {
                var value = int.Parse(singleMatch.Groups[1].Value);
                var unit = singleMatch.Groups[2].Value;
                return unit == "week" ? value * 7 : value;
            }

            // Default fallback: try to extract any number and assume it's days
            var numberMatch = Regex.Match(deliveryTime, @"(\d+)");
            if (numberMatch.Success)
            {
                return int.Parse(numberMatch.Groups[1].Value);
            }

            return null;
        }
    }
}
