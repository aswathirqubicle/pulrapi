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

public class GetShippingAddressQuery : IRequest<ShippingDetailsResponse>
{
    public string Uid { get; set; }
}

public class GetShippingAddressQueryHandler : IRequestHandler<GetShippingAddressQuery, ShippingDetailsResponse>
{
    private readonly ILogger<GetShippingDetailsQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;

    public GetShippingAddressQueryHandler(ILogger<GetShippingDetailsQueryHandler> logger, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    public async Task<ShippingDetailsResponse> Handle(GetShippingAddressQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var shippingAddress = await _dbContext.ShippingDetails
                .Include(sd => sd.CountryNavigation)
                .SingleOrDefaultAsync(sd => sd.IsActive && sd.Uid == request.Uid, cancellationToken);

            if (shippingAddress == null)
                throw new NotFoundException("Address not found");

            return ShippingDetailsResponse.MapFromEntity(shippingAddress);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
