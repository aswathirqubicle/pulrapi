using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;

using System.Collections.Generic;

namespace Core.Application.Mediatr.Orders.Commands.UpdateOrderItemStatus;

public class UpdateOrderItemStatusCommand : IRequest<bool>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();
    
    [Required]
    public string TrackingNumber { get; set; }
    
    [Required]
    public string ShippingProvider { get; set; }
}

public class UpdateOrderItemStatusCommandHandler : IRequestHandler<UpdateOrderItemStatusCommand, bool>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<UpdateOrderItemStatusCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOrderItemStatusCommandHandler(
        IOrderService orderService,
        ILogger<UpdateOrderItemStatusCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateOrderItemStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var result = await _orderService.UpdateOrderItemsStatusAsync(
                user.Id, 
                request.ItemUids, 
                request.TrackingNumber, 
                request.ShippingProvider, 
                cancellationToken);

            _logger.LogInformation("Order items {ItemUids} marked as shipped by user {UserId}", 
                string.Join(", ", request.ItemUids), user.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking order items {ItemUids} as shipped: {Message}", 
                string.Join(", ", request.ItemUids), ex.Message);
            throw;
        }
    }
}
