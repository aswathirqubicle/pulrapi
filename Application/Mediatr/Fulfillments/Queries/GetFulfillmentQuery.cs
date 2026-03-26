using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Fulfillments.Queries;
using Core.Application.Models.Fulfillments;

namespace Core.Application.Mediatr.Fulfillments.Queries
{
    public class GetFulfillmentQuery : IRequest<FulfillmentDetailsResponse>
    {
        [Required]
        public string StoreUid { get; set; }
    }

    public class GetFulfillmentQueryHandler : IRequestHandler<GetFulfillmentQuery, FulfillmentDetailsResponse>
    {
        private readonly ILogger<GetFulfillmentQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetFulfillmentQueryHandler(ILogger<GetFulfillmentQueryHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<FulfillmentDetailsResponse> Handle(GetFulfillmentQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var userStores = await _dbContext.Stores.Where(s => s.User == cUser).ToListAsync();
                var isCurrentUsersFulfillment = await _dbContext.Fulfillments.AnyAsync(f => userStores.Select(s => s.Uid).Contains(request.StoreUid));
                if (!isCurrentUsersFulfillment)
                {
                    return null;
                }

                return await _dbContext.Fulfillments.Where(f => f.Store.Uid == request.StoreUid)
                    .Select(f => new FulfillmentDetailsResponse()
                    {
                        Uid = f.Uid,
                        StoreUid = f.Store.Uid,
                        ReturnCity = f.ReturnCity,
                        ReturnCountryUid = f.ReturnCountry.Uid,
                        ReturnPlaceNumber = f.ReturnPlaceNumber,
                        ReturnAddress = f.ReturnAddress,
                        FulfillmentMethod = f.FulfillmentMethod,
                        PickupCity = f.PickupCity,
                        PickupCountryUid = f.PickupCountry.Uid,
                        PickupPlaceNumber = f.PickupPlaceNumber,
                        PickupAddress = f.PickupAddress,
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
