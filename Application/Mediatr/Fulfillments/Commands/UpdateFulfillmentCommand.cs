using MediatR;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Fulfillments.Commands;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Fulfillments.Commands
{
    public class UpdateFulfillmentCommand : IRequest <Unit>
    {
        [Required]
        public string Uid { get; set; }
        public string PickupCity { get; set; }
        public string PickupPlaceNumber { get; set; }
        public string PickupAddress { get; set; }
        public FulfillmentMethodEnum? FulfillmentMethod { get; set; }
        public string ReturnCity{ get; set; }
        public string ReturnPlaceNumber { get; set; }
        public string ReturnAddress { get; set; }
        public string PickupCountryUid { get; set; }
        public string ReturnCountryUid { get; set; }
    }

    public class UpdateFulfillmentCommandHandler : IRequestHandler<UpdateFulfillmentCommand,Unit>
    {
        private readonly ILogger<UpdateFulfillmentCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public UpdateFulfillmentCommandHandler(ILogger<UpdateFulfillmentCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(UpdateFulfillmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var userStoreUids = await _dbContext.Stores.Where(s => s.User == cUser).Select(s => s.Uid).ToListAsync();

                var fulfillment = await _dbContext.Fulfillments
                                            .SingleOrDefaultAsync(f => f.Uid == request.Uid && userStoreUids.Contains(f.Store.Uid));

                if (fulfillment == null)
                {
                    throw new BadRequestException("Fulfillment doesn't exist");
                }

                fulfillment.PickupPlaceNumber = request.PickupPlaceNumber ?? fulfillment.PickupPlaceNumber;
                fulfillment.PickupAddress = request.PickupAddress ?? fulfillment.PickupAddress;
                fulfillment.PickupCity = request.PickupCity ?? fulfillment.PickupCity;
                fulfillment.FulfillmentMethod = request.FulfillmentMethod ?? fulfillment.FulfillmentMethod;
                fulfillment.ReturnPlaceNumber = request.ReturnPlaceNumber ?? fulfillment.ReturnPlaceNumber;
                fulfillment.ReturnAddress = request.ReturnAddress ?? fulfillment.ReturnAddress;
                fulfillment.ReturnCity = request.ReturnCity ?? fulfillment.ReturnCity;
                fulfillment.PickupCountry = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == request.PickupCountryUid);
                fulfillment.ReturnCountry = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == request.ReturnCountryUid);

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
