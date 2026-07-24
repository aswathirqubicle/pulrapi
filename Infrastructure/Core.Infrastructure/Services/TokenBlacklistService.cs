using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.Infrastructure.Services
{
    /// <summary>
    /// Durable JWT revocation blacklist backed by the database. Storing revoked
    /// <c>jti</c> values in a shared table (rather than an in-memory dictionary)
    /// keeps logout/revocation consistent across process restarts, deployments and
    /// multiple API instances.
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IApplicationDbContext _dbContext;

        public TokenBlacklistService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task BlacklistTokenAsync(string jti, DateTime expiresAtUtc)
        {
            if (string.IsNullOrEmpty(jti))
            {
                return;
            }

            // Guard against duplicate inserts (e.g. a double logout) which would
            // otherwise violate the unique index on Jti.
            var alreadyRevoked = await _dbContext.RevokedTokens
                .AnyAsync(rt => rt.Jti == jti);
            if (alreadyRevoked)
            {
                return;
            }

            _dbContext.RevokedTokens.Add(new RevokedToken
            {
                Jti = jti,
                ExpiresAtUtc = expiresAtUtc
            });

            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            if (string.IsNullOrEmpty(jti))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return await _dbContext.RevokedTokens
                .AnyAsync(rt => rt.Jti == jti && rt.ExpiresAtUtc > now);
        }

        public async Task<int> PurgeExpiredAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await _dbContext.RevokedTokens
                .Where(rt => rt.ExpiresAtUtc <= now)
                .ToListAsync();

            if (expired.Count == 0)
            {
                return 0;
            }

            _dbContext.RevokedTokens.RemoveRange(expired);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return expired.Count;
        }
    }
}
