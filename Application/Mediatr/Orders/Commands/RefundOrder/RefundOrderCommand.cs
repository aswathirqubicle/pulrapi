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

namespace Core.Application.Mediatr.Orders.Commands.RefundOrder;

public class RefundOrderCommand : IRequest<RefundOrderResponse>
{
    [Required]
    public List<string> ItemUids { get; set; } = new();

    public bool Confirmed { get; set; } = false;
}

public class RefundOrderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public bool RequiresConfirmation { get; set; }
    public decimal? TotalRefundAmount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<RefundItemResult> Results { get; set; } = new();
}

public class RefundItemResult
{
    public string ItemUid { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public decimal? RefundAmount { get; set; }
}

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, RefundOrderResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<RefundOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RefundOrderCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<RefundOrderCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<RefundOrderResponse> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
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
                .Include(opa => opa.Product)
                .Include(opa => opa.Order)
                .ThenInclude(o => o.Currency)
                .Where(opa => request.ItemUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            var results = new List<RefundItemResult>();
            var validItems = new List<(OrderProductAffiliate Item, decimal RefundAmount)>();

            // First pass: validate all items and collect refund amounts
            foreach (var itemUid in request.ItemUids)
            {
                var orderItem = orderItems.FirstOrDefault(opa => opa.Uid == itemUid);

                if (orderItem == null)
                {
                    results.Add(new RefundItemResult
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
                    results.Add(new RefundItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = "You are not authorized to refund this order item."
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
                    results.Add(new RefundItemResult
                    {
                        ItemUid = itemUid,
                        Success = false,
                        Message = $"Order item cannot be refunded. Current status: {orderItem.OrderItemStatus}"
                    });
                    continue;
                }

                // Calculate refund amount (product price + shipping cost + 5% VAT)
                const decimal vatRate = 0.05m; // 5% UAE VAT
                var baseAmount = (orderItem.ProductPriceSnapshot ?? 0) + (orderItem.ShippingCostSnapshot ?? 0);
                var vatAmount = baseAmount * vatRate;
                var refundAmount = (baseAmount + vatAmount) * orderItem.ProductQuantity;

                validItems.Add((orderItem, refundAmount));
                results.Add(new RefundItemResult
                {
                    ItemUid = itemUid,
                    Success = true,
                    Message = "Valid for refund.",
                    RefundAmount = refundAmount
                });
            }

            var totalRefundAmount = validItems.Sum(v => v.RefundAmount);

            // If not confirmed, return confirmation response
            if (!request.Confirmed)
            {
                return new RefundOrderResponse
                {
                    Success = true,
                    Message = "Please confirm to proceed with the refund.",
                    RequiresConfirmation = true,
                    TotalRefundAmount = totalRefundAmount,
                    SuccessCount = validItems.Count,
                    FailedCount = results.Count(r => !r.Success),
                    Results = results
                };
            }

            // Process the refunds
            var ordersToUpdate = new Dictionary<int, Order>();

            foreach (var (orderItem, refundAmount) in validItems)
            {
                var order = orderItem.Order;

                // Update order item status to Refunded
                orderItem.OrderItemStatus = OrderStatusEnum.Refunded;
                orderItem.UpdatedAt = DateTime.UtcNow;

                // Track orders for parent status update
                if (!ordersToUpdate.ContainsKey(order.Id))
                {
                    ordersToUpdate[order.Id] = order;
                }

                _logger.LogInformation("Order item {ItemUid} refunded {Amount} to user {UserId}",
                    orderItem.Uid, refundAmount, user.Id);
            }

            // Create wallet refund transactions grouped by seller
            if (validItems.Count > 0)
            {
                var itemsBySeller = validItems
                    .GroupBy(v => v.Item.Product?.UserId)
                    .ToList();

                foreach (var sellerGroup in itemsBySeller)
                {
                    var itemsInGroup = sellerGroup.ToList();
                    var totalSellerAmount = itemsInGroup.Sum(v => v.RefundAmount);
                    var firstItem = itemsInGroup.First().Item;
                    var itemUidsList = string.Join(", ", itemsInGroup.Select(v => v.Item.Uid));

                    var refundTransaction = new WalletTransaction
                    {
                        ProfileId = profile.Id,
                        TransactionType = TransactionTypeEnum.Refund,
                        Amount = totalSellerAmount,
                        CurrencyId = firstItem.Order.CurrencyId,
                        OrderId = firstItem.OrderId,
                        OrderProductAffiliateId = firstItem.Id,
                        Description = $"Refund for order items: {itemUidsList}",
                        TransactionDate = DateTime.UtcNow,
                        Status = TransactionStatusEnum.Completed
                    };
                    _dbContext.WalletTransactions.Add(refundTransaction);
                }
            }

            // Update parent orders status based on all items
            foreach (var orderEntry in ordersToUpdate)
            {
                var order = orderEntry.Value;

                var allOrderItems = await _dbContext.OrderProductAffiliates
                    .Where(opa => opa.OrderId == order.Id && opa.IsActive)
                    .ToListAsync(cancellationToken);

                var allRefunded = allOrderItems.All(item => item.OrderItemStatus == OrderStatusEnum.Refunded);
                if (allRefunded)
                {
                    order.OrderStatus = OrderStatusEnum.Refunded;
                }
                else
                {
                    var anyProcessing = allOrderItems.Any(item =>
                        (item.OrderItemStatus == OrderStatusEnum.Processing &&
                         !(item.CountdownExpiryDate.HasValue && item.CountdownExpiryDate.Value < DateTime.UtcNow)) ||
                        item.OrderItemStatus == OrderStatusEnum.Shipped ||
                        item.OrderItemStatus == OrderStatusEnum.Delivered);
                    var anyFailed = allOrderItems.Any(item =>
                        item.OrderItemStatus == OrderStatusEnum.OrderFailed ||
                        (item.OrderItemStatus == OrderStatusEnum.Processing &&
                         item.CountdownExpiryDate.HasValue &&
                         item.CountdownExpiryDate.Value < DateTime.UtcNow));

                    if (anyProcessing)
                        order.OrderStatus = OrderStatusEnum.Processing;
                    else if (anyFailed)
                        order.OrderStatus = OrderStatusEnum.OrderFailed;
                }

                order.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Update results to show processed status
            foreach (var result in results.Where(r => r.Success))
            {
                result.Message = "Refund processed successfully.";
            }

            var successCount = validItems.Count;
            var failedCount = results.Count(r => !r.Success);

            return new RefundOrderResponse
            {
                Success = successCount > 0,
                Message = successCount > 0
                    ? $"Refund processed. {successCount} item(s) refunded, {failedCount} item(s) failed."
                    : "All refund attempts failed.",
                RequiresConfirmation = false,
                TotalRefundAmount = totalRefundAmount,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order items: {Message}", ex.Message);
            throw;
        }
    }
}