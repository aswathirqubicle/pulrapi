using System;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Infrastructure.Services
{
    public class SettingsCacheService : ISettingsCacheService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMemoryCache _cache;

        private const string PlatformSettingsCacheKey = "platform_settings";
        private const string PaymentFeeSettingCacheKeyPrefix = "payment_fee_setting_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public SettingsCacheService(IApplicationDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<PlatformSetting> GetPlatformSettingsAsync()
        {
            if (_cache.TryGetValue(PlatformSettingsCacheKey, out PlatformSetting cached))
            {
                return cached;
            }

            var settings = await _dbContext.PlatformSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                _cache.Set(PlatformSettingsCacheKey, settings, CacheDuration);
            }

            return settings;
        }

        public async Task<PaymentFeeSetting> GetPaymentFeeSettingAsync(int currencyId)
        {
            var cacheKey = $"{PaymentFeeSettingCacheKeyPrefix}{currencyId}";

            if (_cache.TryGetValue(cacheKey, out PaymentFeeSetting cached))
            {
                return cached;
            }

            var setting = await _dbContext.PaymentFeeSettings
                .FirstOrDefaultAsync(pfs => pfs.CurrencyId == currencyId);

            if (setting != null)
            {
                _cache.Set(cacheKey, setting, CacheDuration);
            }

            return setting;
        }

        public void InvalidatePlatformSettingsCache()
        {
            _cache.Remove(PlatformSettingsCacheKey);
        }

        public void InvalidatePaymentFeeSettingsCache(int currencyId)
        {
            _cache.Remove($"{PaymentFeeSettingCacheKeyPrefix}{currencyId}");
        }

        public void InvalidateAllCache()
        {
            InvalidatePlatformSettingsCache();
        }
    }
}