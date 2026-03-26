using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Core.Infrastructure.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ConcurrentDictionary<string, bool> _blacklistedTokens;

        public TokenBlacklistService(IConfiguration configuration)
        {
            _blacklistedTokens = new ConcurrentDictionary<string, bool>();
        }

        public Task BlacklistTokenAsync(string token)
        {
            _blacklistedTokens.TryAdd(token, true);
            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return Task.FromResult(_blacklistedTokens.ContainsKey(token));
        }

        public Task RemoveTokenAsync(string token)
        {
            _blacklistedTokens.TryRemove(token, out _);
            return Task.CompletedTask;
        }
    }
} 