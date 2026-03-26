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
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.MarkAsShipped;

public class MarkAsShippedCommand : IRequest<bool>
{
    [Required]
    public string OrderUid { get; set; }
    
    [Required]
    public string TrackingNumber { get; set; }
    
    [Required]
    public string ShippingProvider { get; set; }
}

public class MarkAsShippedCommandHandler : IRequestHandler<MarkAsShippedCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<MarkAsShippedCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public MarkAsShippedCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<MarkAsShippedCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(MarkAsShippedCommand request, CancellationToken cancellationToken)
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

            // Load the order with its products
            var order = await _dbContext.Orders
                .Include(o => o.OrderProductAffiliates)
                    .ThenInclude(opa => opa.Product)
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid && o.IsActive, cancellationToken);

            if (order == null)
            {
                throw new NotFoundException($"Order with UID {request.OrderUid} not found.");
            }

            // Verify that the current user is the seller for at least one product in this order
            var isSellerForOrder = order.OrderProductAffiliates.Any(opa => 
                opa.Product != null && opa.Product.UserId == user.Id);

            if (!isSellerForOrder)
            {
                throw new UnauthorizedAccessException("You are not authorized to mark this order as shipped.");
            }

            // Verify order is in Processing status (awaiting delivery)
            if (order.OrderStatus != OrderStatusEnum.Processing)
            {
                throw new BadRequestException($"Order cannot be marked as shipped. Current status: {order.OrderStatus}");
            }

            // Update order with shipping information
            order.TrackingNumber = request.TrackingNumber;
            order.ShippingProvider = request.ShippingProvider;
            order.ShippedAt = DateTime.UtcNow;
            order.OrderStatus = OrderStatusEnum.Shipped;
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderUid} marked as shipped by user {UserId}", request.OrderUid, user.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking order {OrderUid} as shipped: {Message}", request.OrderUid, ex.Message);
            throw;
        }
    }
}
