using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Domain.Enums;

namespace Core.Infrastructure.Services;

public class OrderCountdownService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCountdownService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public OrderCountdownService(IServiceProvider serviceProvider, ILogger<OrderCountdownService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderCountdownService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredCountdownsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired countdowns.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("OrderCountdownService stopped.");
    }

    private async Task ProcessExpiredCountdownsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Query for expired countdown items:
        // - Processing items: countdown expired (seller didn't ship)
        // - Shipped items: countdown expired AND extension expired (if extension was used)
        var expiredItems = await dbContext.OrderProductAffiliates
            .Include(opa => opa.Order)
            .Where(opa => opa.IsActive
                && (opa.OrderItemStatus == OrderStatusEnum.Processing || opa.OrderItemStatus == OrderStatusEnum.Shipped)
                && opa.CountdownExpiryDate != null
                && opa.CountdownExpiryDate < now
                && (opa.ExtensionExpiryDate == null || opa.ExtensionExpiryDate < now))  // Not in active extension period
            .ToListAsync(cancellationToken);

        if (expiredItems.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} order items with expired countdowns.", expiredItems.Count);

        foreach (var item in expiredItems)
        {
            if (item.OrderItemStatus == OrderStatusEnum.Shipped)
            {
                // Shipped items: Only fail if extension has been used and expired
                if (item.ExtensionCount > 0 && (item.ExtensionExpiryDate == null || item.ExtensionExpiryDate < now))
                {
                    item.OrderItemStatus = OrderStatusEnum.OrderFailed;
                    _logger.LogInformation("Shipped order item {ItemUid} status changed to OrderFailed (extension expired).", item.Uid);
                }
                // else: countdown expired but buyer can still extend - don't fail yet
            }
            else if (item.OrderItemStatus == OrderStatusEnum.Processing)
            {
                // Processing items: Fail immediately (seller didn't ship)
                item.OrderItemStatus = OrderStatusEnum.OrderFailed;
                _logger.LogInformation("Order item {ItemUid} status changed to OrderFailed (seller didn't ship).", item.Uid);
            }
        }

        // Update parent order status for each affected order
        var affectedOrderIds = expiredItems.Select(i => i.OrderId).Distinct().ToList();
        foreach (var orderId in affectedOrderIds)
        {
            var order = expiredItems.First(i => i.OrderId == orderId).Order;
            if (order == null) continue;

            var allItems = await dbContext.OrderProductAffiliates
                .Where(opa => opa.OrderId == orderId && opa.IsActive)
                .ToListAsync(cancellationToken);

            // Reflect in-memory updates for items that were just set to OrderFailed
            foreach (var expiredItem in expiredItems.Where(i => i.OrderId == orderId))
            {
                var tracked = allItems.FirstOrDefault(a => a.Id == expiredItem.Id);
                if (tracked != null) tracked.OrderItemStatus = OrderStatusEnum.OrderFailed;
            }

            var anyProcessing = allItems.Any(i =>
                i.OrderItemStatus == OrderStatusEnum.Processing ||
                i.OrderItemStatus == OrderStatusEnum.Shipped ||
                i.OrderItemStatus == OrderStatusEnum.Delivered);

            var allTerminal = allItems.All(i =>
                i.OrderItemStatus == OrderStatusEnum.OrderFailed ||
                i.OrderItemStatus == OrderStatusEnum.Refunded);

            if (!anyProcessing && allTerminal)
            {
                order.OrderStatus = OrderStatusEnum.OrderFailed;
                _logger.LogInformation("Parent order {OrderId} status changed to OrderFailed.", orderId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}