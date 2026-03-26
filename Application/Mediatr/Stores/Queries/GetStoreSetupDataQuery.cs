using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Queries;
using Core.Application.Models.Currencies;
using Core.Application.Models.Stores;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetStoreSetupDataQuery : IRequest<StoreSetupDataResponse>
    {
        [Required] public string Uid { get; set; }
    }

    public class GetStoreSetupDataQueryHandler : IRequestHandler<GetStoreSetupDataQuery, StoreSetupDataResponse>
    {
        private readonly ILogger<GetStoreSetupDataQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetStoreSetupDataQueryHandler(ILogger<GetStoreSetupDataQueryHandler> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<StoreSetupDataResponse> Handle(GetStoreSetupDataQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                return new StoreSetupDataResponse
                {
                    Industries = await _dbContext.Industries
                        .Select(i => new IndustryResponse
                        {
                            Uid = i.Uid,
                            Name = i.Name,
                            Key = i.Key
                        })
                        .OrderBy(s => s.Name)
                        .ToListAsync(cancellationToken),

                    Currencies = await _dbContext.Currencies
                        .Select(i => new CurrencyDetailsResponse
                        {
                            Uid = i.Uid,
                            Name = i.Name,
                            Code = i.Code,
                            Symbol = i.Symbol
                        }).OrderBy(s => s.Name)
                        .ToListAsync(cancellationToken)
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
