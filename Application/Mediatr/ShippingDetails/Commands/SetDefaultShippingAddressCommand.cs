using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Commands;

namespace Core.Application.Mediatr.ShippingDetails.Commands;

public class SetDefaultShippingAddressCommand : IRequest <Unit>
{
    public string Uid { get; set; }
}

public class SetDefaultShippingAddressCommandHandler : IRequestHandler<SetDefaultShippingAddressCommand,Unit>
{
    private readonly ILogger<SetDefaultShippingAddressCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SetDefaultShippingAddressCommandHandler(ILogger<SetDefaultShippingAddressCommandHandler> logger,
        IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(SetDefaultShippingAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

            var shippingAddress =
                await _dbContext.ShippingDetails.SingleOrDefaultAsync(sd => sd.IsActive && sd.Uid == request.Uid && sd.User == cUser,
                    cancellationToken);

            if (shippingAddress == null)
                throw new NotFoundException("Shipping address not found");

            var isBilling = shippingAddress.IsBillingAddress;
            shippingAddress.DefaultShippingAddress = true;

            var otherShippingAddresses = await _dbContext.ShippingDetails
                .Where(sd => sd.IsActive && sd.User == cUser && sd != shippingAddress && sd.IsBillingAddress == isBilling)
                .ToListAsync(cancellationToken);

            foreach (var otherShippingAddress in otherShippingAddresses)
            {
                otherShippingAddress.DefaultShippingAddress = false;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
