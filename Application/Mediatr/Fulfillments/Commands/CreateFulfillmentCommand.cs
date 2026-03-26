using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Fulfillments.Commands;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Fulfillments.Commands
{
    public class CreateFulfillmentCommand : IRequest<string>
    {
        [Required]
        public string StoreUid { get; set; }
        public string PickupCity { get; set; }
        public string PickupPlaceNumber { get; set; }
        public string PickupAddress { get; set; }
        public string PickupCountryUid { get; set; }
        public FulfillmentMethodEnum FulfillmentMethod { get; set; }
        public string ReturnCity { get; set; }
        public string ReturnPlaceNumber { get; set; }
        public string ReturnAddress { get; set; }
        public string ReturnCountryUid { get; set; }
    }

    public class CreateFulfillmentCommandHandler : IRequestHandler<CreateFulfillmentCommand, string>
    {
        private readonly ILogger<CreateFulfillmentCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public CreateFulfillmentCommandHandler(ILogger<CreateFulfillmentCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<string> Handle(CreateFulfillmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var userStoreUids = await _dbContext.Stores.Where(s => s.User == cUser).Select(s => s.Uid).ToListAsync();

                if (!userStoreUids.Contains(request.StoreUid))
                {
                    throw new BadRequestException("Store doesnt exist.");
                }

                var cUserStore = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Uid == request.StoreUid);

                var fullfilmentExists = await _dbContext.Fulfillments.AnyAsync(f => f.Store.Uid == request.StoreUid);
                if (cUserStore == null || fullfilmentExists)
                {
                    throw new BadRequestException(fullfilmentExists ? "Fulfillment already exists" : "Store doesnt exist.");
                }

                var newFulfillment = new Fulfillment()
                {
                    Store = cUserStore,
                    PickupPlaceNumber = request.PickupPlaceNumber,
                    PickupAddress = request.PickupAddress,
                    PickupCity = request.PickupCity,
                    FulfillmentMethod = request.FulfillmentMethod,
                    ReturnPlaceNumber = request.ReturnPlaceNumber,
                    ReturnAddress = request.ReturnAddress,
                    ReturnCity = request.ReturnCity,
                    PickupCountry = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == request.PickupCountryUid),
                    ReturnCountry = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == request.ReturnCountryUid),
                };

                _dbContext.Fulfillments.Add(newFulfillment);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return newFulfillment.Uid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
