using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.PlatformSettings.Commands
{
    public class UpdatePlatformSettingsCommand : IRequest<PlatformSetting>
    {
        public decimal? CommissionRate { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? PlatformFeePercentage { get; set; }
        public decimal? DirectSaleSellerPercentage { get; set; }
        public decimal? CollabSaleSellerPercentage { get; set; }
        public decimal? CollabSaleCreatorPercentage { get; set; }
        public decimal? MinimumWithdrawalAmount { get; set; }
        public int? DeliveryExtensionHours { get; set; }
        public int? RefundWindowDays { get; set; }
        public int? ExchangeWindowDays { get; set; }
        public int? EscrowHoldDays { get; set; }
    }

    public class UpdatePlatformSettingsCommandHandler : IRequestHandler<UpdatePlatformSettingsCommand, PlatformSetting>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ISettingsCacheService _settingsCacheService;

        public UpdatePlatformSettingsCommandHandler(
            IApplicationDbContext dbContext,
            ISettingsCacheService settingsCacheService)
        {
            _dbContext = dbContext;
            _settingsCacheService = settingsCacheService;
        }

        public async Task<PlatformSetting> Handle(UpdatePlatformSettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = await _dbContext.PlatformSettings.FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                settings = new PlatformSetting
                {
                    CommissionRate = 0.01m,
                    VatRate = 0.05m,
                    PlatformFeePercentage = 0.25m,
                    DirectSaleSellerPercentage = 0.75m,
                    CollabSaleSellerPercentage = 0.65m,
                    CollabSaleCreatorPercentage = 0.10m,
                    MinimumWithdrawalAmount = 50.00m,
                    DeliveryExtensionHours = 72,
                    RefundWindowDays = 3,
                    ExchangeWindowDays = 21,
                    EscrowHoldDays = 21
                };
                _dbContext.PlatformSettings.Add(settings);
            }

            if (request.CommissionRate.HasValue)
                settings.CommissionRate = request.CommissionRate.Value;
            if (request.VatRate.HasValue)
                settings.VatRate = request.VatRate.Value;
            if (request.PlatformFeePercentage.HasValue)
                settings.PlatformFeePercentage = request.PlatformFeePercentage.Value;
            if (request.DirectSaleSellerPercentage.HasValue)
                settings.DirectSaleSellerPercentage = request.DirectSaleSellerPercentage.Value;
            if (request.CollabSaleSellerPercentage.HasValue)
                settings.CollabSaleSellerPercentage = request.CollabSaleSellerPercentage.Value;
            if (request.CollabSaleCreatorPercentage.HasValue)
                settings.CollabSaleCreatorPercentage = request.CollabSaleCreatorPercentage.Value;
            if (request.MinimumWithdrawalAmount.HasValue)
                settings.MinimumWithdrawalAmount = request.MinimumWithdrawalAmount.Value;
            if (request.DeliveryExtensionHours.HasValue)
                settings.DeliveryExtensionHours = request.DeliveryExtensionHours.Value;
            if (request.RefundWindowDays.HasValue)
                settings.RefundWindowDays = request.RefundWindowDays.Value;
            if (request.ExchangeWindowDays.HasValue)
                settings.ExchangeWindowDays = request.ExchangeWindowDays.Value;
            if (request.EscrowHoldDays.HasValue)
                settings.EscrowHoldDays = request.EscrowHoldDays.Value;

            settings.UpdatedAt = System.DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _settingsCacheService.InvalidatePlatformSettingsCache();

            return settings;
        }
    }
}