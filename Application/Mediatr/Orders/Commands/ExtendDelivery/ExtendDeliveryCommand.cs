using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Orders;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.ExtendDelivery;

public class ExtendDeliveryCommand : IRequest<ExtendDeliveryResponse>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();
}

public class ExtendDeliveryResponse
{
    public bool Success { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string Message { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ExtendDeliveryItemResult> Results { get; set; } = new();
}

public class ExtendDeliveryItemResult
{
    public string ItemUid { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime? ExtensionExpiryDate { get; set; }
}

public class ExtendDeliveryCommandHandler : IRequestHandler<ExtendDeliveryCommand, ExtendDeliveryResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ExtendDeliveryCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly OrderSettings _orderSettings;

    public ExtendDeliveryCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<ExtendDeliveryCommandHandler> logger,
        ICurrentUserService currentUserService,
        IOptions<OrderSettings> orderSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
        _orderSettings = orderSettings.Value;
    }

    public async Task<ExtendDeliveryResponse> Handle(ExtendDeliveryCommand request, CancellationToken cancellationToken)
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

            // Validate that at least one item UID was provided
            if (request.ItemUids == null || request.ItemUids.Count == 0)
            {
                throw new BadRequestException("At least one item UID must be provided.");
            }

            // Load all order items by UIDs
            var orderItems = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Order)
                    .ThenInclude(o => o.Profile)
                .Where(opa => request.ItemUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            // Use configurable extension hours from settings
            var newExpiryDate = _orderSettings.CalculateExtensionExpiryDate();
            var extensionHours = _orderSettings.DeliveryExtensionHours;
            var results = new List<ExtendDeliveryItemResult>();

            foreach (var itemUid in request.ItemUids)
            {
                var orderItem = orderItems.FirstOrDefault(opa => opa.Uid == itemUid);

                if (orderItem == null)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = $"Order item with UID {itemUid} not found."
                    });
                    continue;
                }

                // Verify buyer ownership
                if (orderItem.Order.ProfileId != profile.Id)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "You are not authorized to extend this order item."
                    });
                    continue;
                }

                // Verify the item was actually shipped (ShippedAt exists)
                if (!orderItem.ShippedAt.HasValue)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "Order item cannot be extended - it was never shipped."
                    });
                    continue;
                }

                // Verify the item is in a valid status for extension (Shipped or OrderFailed after countdown expiry)
                if (orderItem.OrderItemStatus != OrderStatusEnum.Shipped && orderItem.OrderItemStatus != OrderStatusEnum.OrderFailed)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = $"Order item cannot be extended. Current status: {orderItem.OrderItemStatus}"
                    });
                    continue;
                }

                // Block if already refunded
                if (orderItem.OrderItemStatus == OrderStatusEnum.Refunded)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "Order item was refunded and cannot be extended."
                    });
                    continue;
                }

                // Block if already confirmed
                if (orderItem.OrderItemStatus == OrderStatusEnum.Delivered || orderItem.OrderItemStatus == OrderStatusEnum.Completed)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "Order item is already confirmed as delivered."
                    });
                    continue;
                }

                // Check if already extended (only one extension allowed)
                if (orderItem.ExtensionCount >= 1)
                {
                    results.Add(new ExtendDeliveryItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "Delivery has already been extended once. No further extensions allowed."
                    });
                    continue;
                }

                // Apply extension
                orderItem.ExtensionCount = 1;
                orderItem.ExtensionExpiryDate = newExpiryDate;
                orderItem.UpdatedAt = DateTime.UtcNow;

                results.Add(new ExtendDeliveryItemResult
                {
                    ItemUid = itemUid,
                    Success = true,
                    Message = "Delivery extended successfully.",
                    ExtensionExpiryDate = newExpiryDate
                });

                _logger.LogInformation("Order item {ItemUid} delivery extended by user {UserId} until {ExpiryDate}",
                    itemUid, user.Id, newExpiryDate);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var successCount = results.Count(r => r.Success);
            var failedCount = results.Count(r => !r.Success);

            return new ExtendDeliveryResponse
            {
                Success = successCount > 0,
                NewExpiryDate = newExpiryDate,
                Message = successCount > 0
                    ? $"Delivery extended by {extensionHours} hours. {successCount} item(s) succeeded, {failedCount} item(s) failed. New expiry: {newExpiryDate:yyyy-MM-dd HH:mm} UTC"
                    : "All extension attempts failed.",
                SuccessCount = successCount,
                FailedCount = failedCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing delivery extension for order items: {Message}", ex.Message);
            throw;
        }
    }
}