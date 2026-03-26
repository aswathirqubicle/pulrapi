using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.PaymentMethods.Queries;
using Core.Application.Models.PaymentMethods;

namespace Core.Application.Mediatr.PaymentMethods.Queries
{
    public class GetPaymentMethodsQuery : IRequest<List<PaymentMethodResponse>>
    {
    }

    public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, List<PaymentMethodResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetPaymentMethodsQueryHandler> _logger;

        public GetPaymentMethodsQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetPaymentMethodsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<PaymentMethodResponse>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.PaymentMethods.Take(1000).Select(pm => new PaymentMethodResponse() { Name = pm.Name, Uid = pm.Uid }).ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
