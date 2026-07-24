using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;

namespace Core.Application.Mediatr.Orders.Commands.UpdateOrderItemStatus;

public class UpdateOrderItemStatusCommand : IRequest<bool>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();
    
    [Required]
    public string TrackingNumber { get; set; }
    
    [Required]
    public string ShippingProvider { get; set; }

    public List<string> ShippingProofMediaFileUids { get; set; } = new();
}

public class UpdateOrderItemStatusCommandHandler : IRequestHandler<UpdateOrderItemStatusCommand, bool>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<UpdateOrderItemStatusCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public UpdateOrderItemStatusCommandHandler(
        IOrderService orderService,
        ILogger<UpdateOrderItemStatusCommandHandler> logger,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _orderService = orderService;
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
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

            List<int> shippingProofMediaFileIds = new List<int>();
            if (request.ShippingProofMediaFileUids != null && request.ShippingProofMediaFileUids.Any())
            {
                var mediaFiles = await _dbContext.MediaFiles
                    .Where(mf => request.ShippingProofMediaFileUids.Contains(mf.Uid) && mf.IsActive)
                    .ToListAsync(cancellationToken);

                var foundUids = mediaFiles.Select(mf => mf.Uid).ToHashSet();
                var missingUids = request.ShippingProofMediaFileUids.Where(uid => !foundUids.Contains(uid)).ToList();
                if (missingUids.Any())
                {
                    throw new NotFoundException($"Shipping proof media files not found for UIDs: {string.Join(", ", missingUids)}");
                }

                shippingProofMediaFileIds = mediaFiles.Select(mf => mf.Id).ToList();
            }

            var result = await _orderService.UpdateOrderItemsStatusAsync(
                user.Id, 
                request.ItemUids, 
                request.TrackingNumber, 
                request.ShippingProvider,
                shippingProofMediaFileIds,
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