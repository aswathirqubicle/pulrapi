using System.Threading.Tasks;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface ISettingsCacheService
    {
        Task<PlatformSetting> GetPlatformSettingsAsync();
        Task<PaymentFeeSetting> GetPaymentFeeSettingAsync(int currencyId);
        void InvalidatePlatformSettingsCache();
        void InvalidatePaymentFeeSettingsCache(int currencyId);
        void InvalidateAllCache();
    }
}