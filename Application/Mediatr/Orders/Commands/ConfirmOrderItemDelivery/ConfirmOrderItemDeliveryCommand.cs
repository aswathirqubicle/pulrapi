using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;

using System.Collections.Generic;

namespace Core.Application.Mediatr.Orders.Commands.ConfirmOrderItemDelivery;

public class ConfirmOrderItemDeliveryCommand : IRequest<bool>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();
}

public class ConfirmOrderItemDeliveryCommandHandler : IRequestHandler<ConfirmOrderItemDeliveryCommand, bool>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ConfirmOrderItemDeliveryCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmOrderItemDeliveryCommandHandler(
        IOrderService orderService,
        ILogger<ConfirmOrderItemDeliveryCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ConfirmOrderItemDeliveryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var result = await _orderService.ConfirmOrderItemsDeliveryAsync(
                user.Id, 
                request.ItemUids, 
                cancellationToken);

            _logger.LogInformation("Order items {ItemUids} confirmed as delivered by user {UserId}", 
                string.Join(", ", request.ItemUids), user.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming delivery for order items {ItemUids}: {Message}", 
                string.Join(", ", request.ItemUids), ex.Message);
            throw;
        }
    }
}
