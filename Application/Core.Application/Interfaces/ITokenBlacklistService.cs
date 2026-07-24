using System;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// Records a JWT's <c>jti</c> as revoked until the token's own expiry.
        /// </summary>
        Task BlacklistTokenAsync(string jti, DateTime expiresAtUtc);

        /// <summary>
        /// Returns true when the given <c>jti</c> has been revoked and the token
        /// has not yet expired.
        /// </summary>
        Task<bool> IsTokenBlacklistedAsync(string jti);

        /// <summary>
        /// Deletes revoked-token records whose tokens have already expired.
        /// Returns the number of rows removed. Invoked by a recurring job.
        /// </summary>
        Task<int> PurgeExpiredAsync();
    }
}
