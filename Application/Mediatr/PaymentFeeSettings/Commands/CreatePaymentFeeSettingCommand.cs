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
    public class CreatePaymentFeeSettingCommand : IRequest<PaymentFeeSettingResponse>
    {
        [Required]
        public int CurrencyId { get; set; }

        [Required]
        public decimal FeePercentage { get; set; }

        [Required]
        public decimal FixedFee { get; set; }
    }

    public class CreatePaymentFeeSettingCommandHandler : IRequestHandler<CreatePaymentFeeSettingCommand, PaymentFeeSettingResponse>
    {
        private readonly IApplicationDbContext _dbContext;

        public CreatePaymentFeeSettingCommandHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentFeeSettingResponse> Handle(CreatePaymentFeeSettingCommand request, CancellationToken cancellationToken)
        {
            var exists = await _dbContext.PaymentFeeSettings
                .AnyAsync(pfs => pfs.CurrencyId == request.CurrencyId, cancellationToken);

            if (exists)
                throw new System.InvalidOperationException($"Payment fee setting already exists for currency ID {request.CurrencyId}");

            var currency = await _dbContext.Currencies.FindAsync(request.CurrencyId);

            var setting = new PaymentFeeSetting
            {
                CurrencyId = request.CurrencyId,
                FeePercentage = request.FeePercentage,
                FixedFee = request.FixedFee
            };

            _dbContext.PaymentFeeSettings.Add(setting);
            await _dbContext.SaveChangesAsync(cancellationToken);

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