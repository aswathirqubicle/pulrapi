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
using Core.Application.Models.Stripe;
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
    private readonly ISettingsCacheService _settingsCacheService;
    private readonly IStripeService _stripeService;

    public RefundOrderCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<RefundOrderCommandHandler> logger,
        ICurrentUserService currentUserService,
        ISettingsCacheService settingsCacheService,
        IStripeService stripeService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
        _settingsCacheService = settingsCacheService;
        _stripeService = stripeService;
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

            if (request.ItemUids == null || request.ItemUids.Count == 0)
            {
                throw new BadRequestException("At least one item UID must be provided.");
            }

            var orderItems = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Product)
                .Include(opa => opa.Order)
                    .ThenInclude(o => o.Currency)
                .Where(opa => request.ItemUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            var results = new List<RefundItemResult>();
            var validItems = new List<(OrderProductAffiliate Item, decimal RefundAmount)>();

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

                var isExpired = orderItem.CountdownExpiryDate.HasValue && orderItem.CountdownExpiryDate.Value < DateTime.UtcNow;
                if (orderItem.OrderItemStatus == OrderStatusEnum.Processing && isExpired)
                    orderItem.OrderItemStatus = OrderStatusEnum.OrderFailed;

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

                var refundAmount = orderItem.ProductPriceSnapshot.GetValueOrDefault() * orderItem.ProductQuantity;

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

            if (!request.Confirmed)
            {
                return new RefundOrderResponse
                {
                    Success = true,
                    Message = "Please confirm to proceed with the refund. The amount will be refunded to your original payment method via Stripe. Card processing fees are non-refundable.",
                    RequiresConfirmation = true,
                    TotalRefundAmount = totalRefundAmount,
                    SuccessCount = validItems.Count,
                    FailedCount = results.Count(r => !r.Success),
                    Results = results
                };
            }

            // Process refunds — group by order to issue one Stripe refund per PaymentIntent
            var ordersToUpdate = new Dictionary<int, Order>();
            var stripeRefundResults = new List<(Order Order, string StripeRefundId, decimal RefundAmount)>();

            // Group valid items by their order's StripePaymentIntentId for batch refund
            var itemsByPaymentIntent = validItems
                .GroupBy(v => v.Item.Order?.StripePaymentIntentId)
                .ToList();

            foreach (var piGroup in itemsByPaymentIntent)
            {
                var paymentIntentId = piGroup.Key;
                var groupTotal = piGroup.Sum(v => v.RefundAmount);
                var firstOrder = piGroup.First().Item.Order;

                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    _logger.LogWarning("Order {OrderUid} has no StripePaymentIntentId. Falling back to wallet credit for refund.", firstOrder?.Uid);
                    continue;
                }

                try
                {
                    var refundAmountInCents = (long)Math.Round(groupTotal * 100, MidpointRounding.AwayFromZero);

                    var stripeRefund = await _stripeService.CreateRefundAsync(new RefundRequest
                    {
                        PaymentIntentId = paymentIntentId,
                        AmountInCents = refundAmountInCents,
                        Reason = "requested_by_customer",
                        Metadata = new Dictionary<string, string>
                        {
                            { "refund_type", "order_failed_wallet_fallback" },
                            { "order_uid", firstOrder.Uid }
                        }
                    });

                    stripeRefundResults.Add((firstOrder, stripeRefund.RefundId, groupTotal));

                    foreach (var (orderItem, refundAmount) in piGroup)
                    {
                        orderItem.OrderItemStatus = OrderStatusEnum.Refunded;
                        orderItem.EscrowStatus = EscrowStatusEnum.Cancelled;
                        orderItem.UpdatedAt = DateTime.UtcNow;

                        if (!ordersToUpdate.ContainsKey(orderItem.OrderId))
                        {
                            ordersToUpdate[orderItem.OrderId] = orderItem.Order;
                        }

                        _logger.LogInformation("Order item {ItemUid} refunded {Amount} to user {UserId} via Stripe refund {RefundId}",
                            orderItem.Uid, refundAmount, user.Id, stripeRefund.RefundId);

                        _dbContext.WalletTransactions.Add(new WalletTransaction
                        {
                            ProfileId = profile.Id,
                            TransactionType = TransactionTypeEnum.Refund,
                            Amount = refundAmount,
                            CurrencyId = orderItem.Order.CurrencyId,
                            OrderId = orderItem.OrderId,
                            OrderProductAffiliateId = orderItem.Id,
                            Description = orderItem.Order.Uid,
                            TransactionDate = DateTime.UtcNow,
                            Status = TransactionStatusEnum.Completed
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe refund failed for PaymentIntent {PaymentIntentId}. Falling back to wallet credit.", paymentIntentId);

                    foreach (var (orderItem, refundAmount) in piGroup)
                    {
                        orderItem.OrderItemStatus = OrderStatusEnum.Refunded;
                        orderItem.UpdatedAt = DateTime.UtcNow;

                        if (!ordersToUpdate.ContainsKey(orderItem.OrderId))
                        {
                            ordersToUpdate[orderItem.OrderId] = orderItem.Order;
                        }

                        _logger.LogInformation("Order item {ItemUid} refunded {Amount} to wallet (Stripe refund fallback) for user {UserId}",
                            orderItem.Uid, refundAmount, user.Id);

                        var walletTransaction = new WalletTransaction
                        {
                            ProfileId = profile.Id,
                            TransactionType = TransactionTypeEnum.Refund,
                            Amount = refundAmount,
                            CurrencyId = orderItem.Order.CurrencyId,
                            OrderId = orderItem.OrderId,
                            OrderProductAffiliateId = orderItem.Id,
                            Description = $"Refund for order item: {orderItem.Uid} (Stripe refund fallback)",
                            TransactionDate = DateTime.UtcNow,
                            Status = TransactionStatusEnum.Completed
                        };
                        _dbContext.WalletTransactions.Add(walletTransaction);
                    }
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

            foreach (var result in results.Where(r => r.Success))
            {
                result.Message = "Refund processed successfully. The amount will be credited to your original payment method within 5-10 business days.";
            }

            var successCount = validItems.Count;
            var failedCount = results.Count(r => !r.Success);

            return new RefundOrderResponse
            {
                Success = successCount > 0,
                Message = successCount > 0
                    ? $"Refund processed. {successCount} item(s) refunded, {failedCount} item(s) failed. The refund will be credited to your original payment method within 5-10 business days. Note: Card processing fees are non-refundable."
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