using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.ConfirmDelivery;

public class ConfirmDeliveryCommand : IRequest<bool>
{
    [Required]
    public string OrderUid { get; set; }
}

public class ConfirmDeliveryCommandHandler : IRequestHandler<ConfirmDeliveryCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ConfirmDeliveryCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmDeliveryCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<ConfirmDeliveryCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ConfirmDeliveryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (profile == null)
            {
                throw new NotFoundException("Profile not found.");
            }

            // Load the order
            var order = await _dbContext.Orders
                .Include(o => o.Profile)
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid && o.IsActive, cancellationToken);

            if (order == null)
            {
                throw new NotFoundException($"Order with UID {request.OrderUid} not found.");
            }

            // Verify that the current user is the buyer (order owner)
            if (order.ProfileId != profile.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to confirm delivery for this order.");
            }

            // Verify order is in Shipped status
            if (order.OrderStatus != OrderStatusEnum.Shipped)
            {
                throw new BadRequestException($"Order cannot be confirmed as delivered. Current status: {order.OrderStatus}");
            }

            // Update order status to Completed
            order.OrderStatus = OrderStatusEnum.Completed;
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderUid} confirmed as delivered by user {UserId}", request.OrderUid, user.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming delivery for order {OrderUid}: {Message}", request.OrderUid, ex.Message);
            throw;
        }
    }
}
