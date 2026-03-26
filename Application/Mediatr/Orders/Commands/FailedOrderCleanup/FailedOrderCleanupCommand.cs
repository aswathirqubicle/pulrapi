using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.FailedOrderCleanup;

public class FailedOrderCleanupCommand : IRequest<bool>
{
    public string OrderUid { get; set; }
}

public class FailedOrderCleanupCommandHandler : IRequestHandler<FailedOrderCleanupCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<FailedOrderCleanupCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public FailedOrderCleanupCommandHandler(
        IApplicationDbContext dbContext, 
        ILogger<FailedOrderCleanupCommandHandler> logger, 
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(FailedOrderCleanupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(true);
            
            var order = await _dbContext.Orders
                .Include(o => o.OrderProductAffiliates)
                    .ThenInclude(opa => opa.Product)
                .Include(o => o.Profile)
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid && o.IsActive, cancellationToken);

            if (order == null)
            {
                throw new NotFoundException($"Order with UID {request.OrderUid} not found or already inactive.");
            }

            // Ensure the order belongs to the current user
            if (order.Profile.UserId != user.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to cleanup this order.");
            }

            foreach (var item in order.OrderProductAffiliates)
            {
                // 1. Restore product quantities for variants
                if (item.ProductVariantCombinationId.HasValue)
                {
                    var variantCombination = await _dbContext.ProductVariantCombinations
                        .FirstOrDefaultAsync(vc => vc.Id == item.ProductVariantCombinationId.Value, cancellationToken);
                    
                    if (variantCombination != null)
                    {
                        variantCombination.Quantity += item.ProductQuantity;
                        variantCombination.IsAvailable = true;
                    }
                }

                // 2. Add product back to the user's bag
                var existingBagItem = await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == item.ProductId
                        && (string.IsNullOrEmpty(item.ProductVariantCombinationUidSnapshot) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == item.ProductVariantCombinationUidSnapshot),
                        cancellationToken);

                if (existingBagItem != null)
                {
                    existingBagItem.Quantity += item.ProductQuantity;
                    existingBagItem.IsActive = true;
                    existingBagItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var bagItem = new UserBagProduct
                    {
                        UserId = user.Id,
                        BagProductId = item.ProductId,
                        Quantity = item.ProductQuantity,
                        ProductVariantCombinationUid = item.ProductVariantCombinationUidSnapshot,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _dbContext.UserBagProducts.Add(bagItem);
                }
                
                // Soft delete order item
                item.IsActive = false;
                item.OrderItemStatus = OrderStatusEnum.Rejected;
            }

            // 3. Update related wallet transactions to Failed
            var transactions = await _dbContext.WalletTransactions
                .Where(t => t.OrderId == order.Id)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                transaction.Status = TransactionStatusEnum.Failed;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            // 4. Soft delete order
            order.IsActive = false;
            order.OrderStatus = OrderStatusEnum.Rejected;
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully cleaned up failed order {OrderUid}, restored items to bag, and marked transactions as failed.", request.OrderUid);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error cleaning up failed order {OrderUid}: {Message}", request.OrderUid, e.Message);
            throw;
        }
    }
}
