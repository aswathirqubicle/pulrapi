using Core.Domain.Common;
using System;

namespace Core.Domain.Entities
{
    /// <summary>
    /// Durable record of a revoked (logged-out) JWT access token, keyed by the
    /// token's <c>jti</c> claim. Persisting this in the database (instead of an
    /// in-memory dictionary) keeps revocation consistent across process restarts,
    /// deployments and multiple API instances. Rows are purged after the token's
    /// own expiry by a recurring cleanup job.
    /// </summary>
    public class RevokedToken : EntityBase
    {
        /// <summary>The JWT ID (<c>jti</c>) claim that has been revoked.</summary>
        public string Jti { get; set; }

        /// <summary>Owning user id, kept for auditing. Optional.</summary>
        public string UserId { get; set; }

        /// <summary>Original token expiry; once passed the row can be purged.</summary>
        public DateTime ExpiresAtUtc { get; set; }
    }
}
