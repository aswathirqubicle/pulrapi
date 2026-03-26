using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Currencies.Queries;
using Core.Application.Models.Currencies;

namespace Core.Application.Mediatr.Currencies.Queries
{
    public class GetCurrenciesQuery : IRequest<AllCurrenciesResponse>
    {
    }

    public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, AllCurrenciesResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetCurrenciesQueryHandler> _logger;
        private readonly IMapper _mapper;

        public GetCurrenciesQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetCurrenciesQueryHandler> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<AllCurrenciesResponse> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var allCurrencies = new AllCurrenciesResponse();
                var currenciesRes = await _dbContext.Currencies.Take(1000).ToListAsync();
                allCurrencies.Currencies = _mapper.Map<List<CurrencyDetailsResponse>>(currenciesRes);
                allCurrencies.MainCurrency = await _dbContext.GlobalCurrencySettings.Select(gcs => _mapper.Map<CurrencyDetailsResponse>(gcs.BaseCurrency)).SingleOrDefaultAsync();

                return allCurrencies;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
