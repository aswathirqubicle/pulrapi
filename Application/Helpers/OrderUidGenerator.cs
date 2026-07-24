using System;

namespace Core.Application.Helpers
{
    /// <summary>
    /// Generates public order identifiers (the order <c>Uid</c>).
    /// The value is intentionally non-sequential and non-guessable so that
    /// authenticated users cannot enumerate other users' orders (IDOR).
    /// </summary>
    public static class OrderUidGenerator
    {
        /// <summary>
        /// Builds a non-sequential public order id, e.g. <c>P20260629T143012-9F3A1C8B</c>.
        /// A UTC timestamp keeps values roughly ordered and unique per second, while
        /// the random GUID suffix removes any predictability.
        /// </summary>
        public static string Generate()
        {
            var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"P{DateTime.UtcNow:yyyyMMddTHHmmss}-{random}";
        }
    }
}
