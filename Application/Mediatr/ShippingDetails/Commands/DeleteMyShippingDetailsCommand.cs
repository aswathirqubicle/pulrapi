using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Commands;

namespace Core.Application.Mediatr.ShippingDetails.Commands;

public class DeleteMyShippingDetailsCommand : IRequest <Unit>
{
    [Required] public string Uid { get; set; }
}

public class DeleteMyShippingDetailsCommandHandler : IRequestHandler<DeleteMyShippingDetailsCommand,Unit>
{
    private readonly ILogger<DeleteMyShippingDetailsCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public DeleteMyShippingDetailsCommandHandler(
        ILogger<DeleteMyShippingDetailsCommandHandler> logger,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(DeleteMyShippingDetailsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

            var shippingAddress = await _dbContext.ShippingDetails.SingleOrDefaultAsync(sd => sd.IsActive
                && sd.Uid == request.Uid
                && sd.User == cUser, cancellationToken);

            if (shippingAddress != null)
            {
                // Always soft delete for consistency and to preserve order history
                shippingAddress.IsActive = false;
                shippingAddress.UserId = null; // Unlink from user so it doesn't show in their address list
                shippingAddress.DefaultShippingAddress = false;
                
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
