using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Settings;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.PaymentFeeSettings.Commands
{
    public class UpdatePaymentFeeSettingCommand : IRequest<PaymentFeeSettingResponse>
    {
        [Required]
        public string Uid { get; set; }

        public decimal? FeePercentage { get; set; }

        public decimal? FixedFee { get; set; }
    }

    public class UpdatePaymentFeeSettingCommandHandler : IRequestHandler<UpdatePaymentFeeSettingCommand, PaymentFeeSettingResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ISettingsCacheService _settingsCacheService;

        public UpdatePaymentFeeSettingCommandHandler(
            IApplicationDbContext dbContext,
            ISettingsCacheService settingsCacheService)
        {
            _dbContext = dbContext;
            _settingsCacheService = settingsCacheService;
        }

        public async Task<PaymentFeeSettingResponse> Handle(UpdatePaymentFeeSettingCommand request, CancellationToken cancellationToken)
        {
            var setting = await _dbContext.PaymentFeeSettings
                .FirstOrDefaultAsync(pfs => pfs.Uid == request.Uid, cancellationToken);

            if (setting == null)
                throw new System.InvalidOperationException($"Payment fee setting with Uid '{request.Uid}' not found");

            if (request.FeePercentage.HasValue)
                setting.FeePercentage = request.FeePercentage.Value;
            if (request.FixedFee.HasValue)
                setting.FixedFee = request.FixedFee.Value;

            setting.UpdatedAt = System.DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _settingsCacheService.InvalidatePaymentFeeSettingsCache(setting.CurrencyId);

            var currency = await _dbContext.Currencies.FindAsync(setting.CurrencyId);

            return new PaymentFeeSettingResponse
            {
                Uid = setting.Uid,
                CurrencyId = setting.CurrencyId,
                CurrencyCode = currency?.Code ?? "",
                FeePercentage = setting.FeePercentage,
                FixedFee = setting.FixedFee
            };
        }
    }
}