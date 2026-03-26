using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Mediatr.ShippingDetails.Queries;

public class GetDefaultShippingAddressQuery : IRequest<ShippingDetailsResponse>
{
    public bool IsBillingAddress { get; set; } = false;
}

public class GetDefaultShippingAddressQueryHandler : IRequestHandler<GetDefaultShippingAddressQuery, ShippingDetailsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetDefaultShippingAddressQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetDefaultShippingAddressQueryHandler(IApplicationDbContext dbContext,
        ILogger<GetDefaultShippingAddressQueryHandler> logger, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<ShippingDetailsResponse> Handle(GetDefaultShippingAddressQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync(skipDetails: true);
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

            var shippingAddress = await _dbContext.ShippingDetails
                .Include(sd => sd.CountryNavigation)
                .SingleOrDefaultAsync(sd => sd.IsActive
                    && sd.DefaultShippingAddress && sd.UserId == cUser.Id && sd.IsBillingAddress == request.IsBillingAddress, cancellationToken);

            if (shippingAddress == null)
                throw new NotFoundException("Shipping address was not found");

            return ShippingDetailsResponse.MapFromEntity(shippingAddress);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
