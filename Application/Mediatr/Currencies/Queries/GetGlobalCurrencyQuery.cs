using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Currencies.Queries;
using Core.Application.Models.Currencies;

namespace Core.Application.Mediatr.Currencies.Queries
{
    public class GetGlobalCurrencyQuery : IRequest<CurrencyDetailsResponse>
    {
    }

    public class GetGlobalCurrencyQueryHandler : IRequestHandler<GetGlobalCurrencyQuery, CurrencyDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetGlobalCurrencyQueryHandler> _logger;

        public GetGlobalCurrencyQueryHandler(IApplicationDbContext dbContext, ILogger<GetGlobalCurrencyQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CurrencyDetailsResponse> Handle(GetGlobalCurrencyQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.GlobalCurrencySettings.Select(gcs => new CurrencyDetailsResponse()
                {
                    Code = gcs.BaseCurrency.Code,
                    Name = gcs.BaseCurrency.Name,
                    Symbol = gcs.BaseCurrency.Symbol,
                    Uid = gcs.BaseCurrency.Uid,
                }).SingleOrDefaultAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
