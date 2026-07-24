using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Settings;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.PaymentFeeSettings.Queries
{
    public class GetPaymentFeeSettingByCurrencyQuery : IRequest<PaymentFeeSettingResponse>
    {
        public int CurrencyId { get; set; }
    }

    public class GetPaymentFeeSettingByCurrencyQueryHandler : IRequestHandler<GetPaymentFeeSettingByCurrencyQuery, PaymentFeeSettingResponse>
    {
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly IApplicationDbContext _dbContext;

        public GetPaymentFeeSettingByCurrencyQueryHandler(
            ISettingsCacheService settingsCacheService,
            IApplicationDbContext dbContext)
        {
            _settingsCacheService = settingsCacheService;
            _dbContext = dbContext;
        }

        public async Task<PaymentFeeSettingResponse> Handle(GetPaymentFeeSettingByCurrencyQuery request, CancellationToken cancellationToken)
        {
            var setting = await _settingsCacheService.GetPaymentFeeSettingAsync(request.CurrencyId);

            if (setting == null)
                return null;

            var currency = await _dbContext.Currencies.FindAsync(request.CurrencyId);

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