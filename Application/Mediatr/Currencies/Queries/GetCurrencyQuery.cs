using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Currencies.Queries;
using Core.Application.Models.Currencies;

namespace Core.Application.Mediatr.Currencies.Queries
{
    public class GetCurrencyQuery : IRequest<CurrencyDetailsResponse>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class GetCurrencyQueryHandler : IRequestHandler<GetCurrencyQuery, CurrencyDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetCurrencyQueryHandler> _logger;
        private readonly IMapper _mapper;

        public GetCurrencyQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetCurrencyQueryHandler> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<CurrencyDetailsResponse> Handle(GetCurrencyQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var currencyRes = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == request.Uid);
                return _mapper.Map<CurrencyDetailsResponse>(currencyRes);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
