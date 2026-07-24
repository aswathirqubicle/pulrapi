using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Settings;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.PaymentFeeSettings.Queries
{
    public class GetPaymentFeeSettingsQuery : IRequest<List<PaymentFeeSettingResponse>>
    {
    }

    public class GetPaymentFeeSettingsQueryHandler : IRequestHandler<GetPaymentFeeSettingsQuery, List<PaymentFeeSettingResponse>>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetPaymentFeeSettingsQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PaymentFeeSettingResponse>> Handle(GetPaymentFeeSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _dbContext.PaymentFeeSettings
                .Include(pfs => pfs.Currency)
                .Where(pfs => pfs.IsActive)
                .ToListAsync(cancellationToken);

            return settings.Select(s => new PaymentFeeSettingResponse
            {
                Uid = s.Uid,
                CurrencyId = s.CurrencyId,
                CurrencyCode = s.Currency?.Code ?? "",
                FeePercentage = s.FeePercentage,
                FixedFee = s.FixedFee
            }).ToList();
        }
    }
}