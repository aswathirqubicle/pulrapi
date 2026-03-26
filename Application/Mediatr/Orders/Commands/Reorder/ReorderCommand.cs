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
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.Reorder;

public class ReorderCommand : IRequest<ReorderResponse>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();
}

public class ReorderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ReorderItemResult> Results { get; set; } = new();
}

public class ReorderItemResult
{
    public string ItemUid { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime? NewCountdownExpiryDate { get; set; }
}

public class ReorderCommandHandler : IRequestHandler<ReorderCommand, ReorderResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ReorderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ReorderCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<ReorderCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<ReorderResponse> Handle(ReorderCommand request, CancellationToken cancellationToken)
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
                .Include(opa => opa.Product)
                .Where(opa => request.ItemUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            var results = new List<ReorderItemResult>();
            var ordersToUpdate = new Dictionary<int, Order>();

            foreach (var itemUid in request.ItemUids)
            {
                var orderItem = orderItems.FirstOrDefault(opa => opa.Uid == itemUid);

                if (orderItem == null)
                {
                    results.Add(new ReorderItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = $"Order item with UID {itemUid} not found."
                    });
                    continue;
                }

                var order = orderItem.Order;

                // Verify that the current user is the buyer (order owner)
                if (order.ProfileId != profile.Id)
                {
                    results.Add(new ReorderItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "You are not authorized to reorder this order item."
                    });
                    continue;
                }

                // Lazy update: if countdown expired but background job hasn't run yet, mark as OrderFailed now
                var isExpired = orderItem.CountdownExpiryDate.HasValue && orderItem.CountdownExpiryDate.Value < DateTime.UtcNow;
                if (orderItem.OrderItemStatus == OrderStatusEnum.Processing && isExpired)
                    orderItem.OrderItemStatus = OrderStatusEnum.OrderFailed;

                // Verify order item is in OrderFailed status
                if (orderItem.OrderItemStatus != OrderStatusEnum.OrderFailed)
                {
                    results.Add(new ReorderItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = $"Order item cannot be reordered. Current status: {orderItem.OrderItemStatus}"
                    });
                    continue;
                }

                // Check if retry is allowed (only one retry allowed)
                if (!orderItem.IsRetryAllowed || orderItem.RetryCount >= 1)
                {
                    results.Add(new ReorderItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "Reorder is not allowed. You have already used your retry attempt."
                    });
                    continue;
                }

                // Calculate new countdown expiry date based on product's delivery time
                double deliveryDays = 7; // Default to 7 days
                if (!string.IsNullOrEmpty(orderItem.DeliveryTimeSnapshot))
                {
                    var dt = orderItem.DeliveryTimeSnapshot.ToLower().Trim();
                    var rangeMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*-\s*(\d+)\s*(day|week)s?");
                    if (rangeMatch.Success)
                        deliveryDays = rangeMatch.Groups[3].Value == "week"
                            ? int.Parse(rangeMatch.Groups[2].Value) * 7
                            : int.Parse(rangeMatch.Groups[2].Value);
                    else
                    {
                        var singleMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*(day|week)s?");
                        if (singleMatch.Success)
                            deliveryDays = singleMatch.Groups[2].Value == "week"
                                ? int.Parse(singleMatch.Groups[1].Value) * 7
                                : int.Parse(singleMatch.Groups[1].Value);
                        else
                        {
                            var minuteMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*(minute|min)s?");
                            if (minuteMatch.Success)
                                deliveryDays = double.Parse(minuteMatch.Groups[1].Value) / 1440.0;
                            else
                            {
                                var hourMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*hours?");
                                if (hourMatch.Success)
                                    deliveryDays = double.Parse(hourMatch.Groups[1].Value) / 24.0;
                                else
                                {
                                    var numMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)");
                                    if (numMatch.Success) deliveryDays = int.Parse(numMatch.Groups[1].Value);
                                }
                            }
                        }
                    }
                }

                var newCountdownExpiryDate = DateTime.UtcNow.AddDays(deliveryDays);

                // Update order item
                orderItem.RetryCount += 1;
                orderItem.IsRetryAllowed = false;
                orderItem.OrderItemStatus = OrderStatusEnum.Processing;
                orderItem.NewCountdownExpiryDate = newCountdownExpiryDate;
                orderItem.CountdownExpiryDate = newCountdownExpiryDate;
                orderItem.UpdatedAt = DateTime.UtcNow;

                // Track orders for parent status update
                if (!ordersToUpdate.ContainsKey(order.Id))
                {
                    ordersToUpdate[order.Id] = order;
                }

                results.Add(new ReorderItemResult
                {
                    ItemUid = itemUid,
                    Success = true,
                    Message = "Reorder processed successfully.",
                    NewCountdownExpiryDate = newCountdownExpiryDate
                });

                _logger.LogInformation("Order item {ItemUid} reordered by user {UserId}. New countdown: {NewCountdown}",
                    itemUid, user.Id, newCountdownExpiryDate);
            }

            // Update parent orders if needed
            foreach (var orderEntry in ordersToUpdate)
            {
                var order = orderEntry.Value;
                // Current item is now Processing — if parent was OrderFailed, update it
                if (order.OrderStatus == OrderStatusEnum.OrderFailed)
                {
                    order.OrderStatus = OrderStatusEnum.Processing;
                }
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var successCount = results.Count(r => r.Success);
            var failedCount = results.Count(r => !r.Success);

            return new ReorderResponse
            {
                Success = successCount > 0,
                Message = successCount > 0
                    ? $"Reorder processed. {successCount} item(s) succeeded, {failedCount} item(s) failed."
                    : "All reorder attempts failed.",
                SuccessCount = successCount,
                FailedCount = failedCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reorder for order items: {Message}", ex.Message);
            throw;
        }
    }
}