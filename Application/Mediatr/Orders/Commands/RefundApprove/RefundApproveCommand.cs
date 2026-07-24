using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Stripe;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Commands.RefundApprove
{
    public class RefundApproveCommand : IRequest<RefundApproveResponse>
    {
        [Required]
        public string OrderUid { get; set; }

        [Required]
        public string ProductOrderUid { get; set; }
    }

    public class RefundApproveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ProductOrderUid { get; set; }
        public decimal RefundAmount { get; set; }
        public string StripeRefundId { get; set; }
        public string EscrowStatus { get; set; }
    }

    public class RefundApproveCommandHandler : IRequestHandler<RefundApproveCommand, RefundApproveResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStripeService _stripeService;
        private readonly ILogger<RefundApproveCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public RefundApproveCommandHandler(
            IApplicationDbContext dbContext,
            IStripeService stripeService,
            ILogger<RefundApproveCommandHandler> logger,
            ICurrentUserService currentUserService,
            ISettingsCacheService settingsCacheService,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _dbContext = dbContext;
            _stripeService = stripeService;
            _logger = logger;
            _currentUserService = currentUserService;
            _settingsCacheService = settingsCacheService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<RefundApproveResponse> Handle(RefundApproveCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var order = await _dbContext.Orders
                .Include(o => o.Currency)
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid, cancellationToken);

            if (order == null)
            {
                throw new NotFoundException($"Order with UID {request.OrderUid} not found.");
            }

            var orderItem = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Product)
                .FirstOrDefaultAsync(opa => opa.Uid == request.ProductOrderUid && opa.OrderId == order.Id && opa.IsActive, cancellationToken);

            if (orderItem == null)
            {
                throw new NotFoundException($"Order item with UID {request.ProductOrderUid} not found.");
            }

            var sellerUserId = orderItem.Product?.UserId;
            if (sellerUserId == null)
            {
                throw new BadRequestException("Product seller information not found for this order item.");
            }

            var sellerProfile = await _dbContext.Profiles.FirstOrDefaultAsync(
                p => p.UserId == sellerUserId, cancellationToken);

            if (sellerProfile == null || sellerProfile.UserId != user.Id)
            {
                throw new ForbiddenException("Only the seller can approve a refund request.");
            }

            if (orderItem.EscrowStatus != EscrowStatusEnum.RefundRequested)
            {
                throw new BadRequestException($"Cannot approve refund. Current escrow status: {orderItem.EscrowStatus}. Expected: RefundRequested.");
            }

            var refundDispute = await _dbContext.Set<RefundDispute>()
                .FirstOrDefaultAsync(
                    rd => rd.OrderProductAffiliateId == orderItem.Id
                        && rd.Status == DisputeStatusEnum.Pending,
                    cancellationToken);

            if (refundDispute == null)
            {
                throw new NotFoundException("Pending refund dispute not found for this order item.");
            }

            if (string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                throw new BadRequestException("No Stripe payment intent found for this order. Cannot process refund.");
            }

            long refundAmountInCents;
            decimal refundAmount;

            // Use the item-level price snapshot captured at order time, not the full order total.
            // This prevents a seller approving one item from refunding the entire multi-item order.
            // Never fall back to order.Amount: a single item must never refund the whole order
            // (that would fully consume the shared PaymentIntent and block other items' refunds).
            refundAmount = (orderItem.ProductPriceSnapshot ?? 0m) * orderItem.ProductQuantity;
            if (refundAmount <= 0m)
            {
                throw new BadRequestException("Cannot determine refund amount for this item. Item price snapshot is missing or zero.");
            }
            refundAmountInCents = (long)Math.Round(refundAmount * 100, MidpointRounding.AwayFromZero);

            if (refundAmountInCents <= 0)
            {
                throw new BadRequestException("Refund amount must be greater than zero.");
            }

            _logger.LogInformation("Processing Stripe refund for OrderItem {ItemUid}, PaymentIntent: {PaymentIntentId}, Amount: {Amount} cents ({CurrencyAmount})",
                orderItem.Uid, order.StripePaymentIntentId, refundAmountInCents, refundAmount);

            RefundResponse stripeRefund;
            try
            {
                var refundRequest = new RefundRequest
                {
                    PaymentIntentId = order.StripePaymentIntentId,
                    AmountInCents = refundAmountInCents,
                    Reason = "requested_by_customer",
                    Metadata = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "order_product_affiliate_uid", orderItem.Uid },
                        { "refund_type", "seller_approved" },
                        { "net_amount", refundAmount.ToString("F2") }
                    }
                };

                stripeRefund = await _stripeService.CreateRefundAsync(refundRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe refund failed for OrderItem {ItemUid}, PaymentIntent: {PaymentIntentId}",
                    orderItem.Uid, order.StripePaymentIntentId);
                throw new BadRequestException($"Stripe refund failed: {ex.Message}. Please try again later.");
            }

            refundDispute.Status = DisputeStatusEnum.Resolved;
            refundDispute.AdminResolvedAt = DateTime.UtcNow;
            refundDispute.ResolvedByAdminUserId = user.Id;
            refundDispute.UpdatedAt = DateTime.UtcNow;

            var escrowTx = await _dbContext.Set<EscrowWalletTransaction>()
                .FirstOrDefaultAsync(
                    ewt => ewt.OrderProductAffiliateId == orderItem.Id
                        && ewt.Status == EscrowWalletTransactionStatusEnum.Active,
                    cancellationToken);

            if (escrowTx != null)
            {
                escrowTx.Status = EscrowWalletTransactionStatusEnum.Refunded;
                escrowTx.StripeRefundId = stripeRefund.RefundId;
                escrowTx.UpdatedAt = DateTime.UtcNow;

                // Unwind the SELLER share that was actually locked at order time.
                // The LOCK only locked SellerAmount, not the gross Amount, so subtracting
                // Amount here would over-decrement the locked balance.
                var sellerLocked = escrowTx.SellerAmount ?? 0m;

                var escrowWallet = await _dbContext.Set<EscrowWallet>()
                    .FirstOrDefaultAsync(ew => ew.Id == escrowTx.EscrowWalletId, cancellationToken);

                if (escrowWallet != null)
                {
                    escrowWallet.LockedBalance -= sellerLocked;
                    escrowWallet.UpdatedAt = DateTime.UtcNow;
                }

                // Mirror onto the AspNetUsers.EscrowBalance rollup (not mapped in EF here; raw SQL).
                if (sellerLocked > 0m && sellerUserId != null)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE \"AspNetUsers\" SET \"EscrowBalance\" = GREATEST(\"EscrowBalance\" - {0}, 0) WHERE \"Id\" = {1}",
                        new object[] { sellerLocked, sellerUserId }, cancellationToken);
                }

                // Collab sale: also unwind the creator's locked share.
                if (escrowTx.IsCollabSale && (escrowTx.CreatorAmount ?? 0m) > 0m && escrowTx.CreatorUserId != null)
                {
                    var creatorAmount = escrowTx.CreatorAmount.Value;

                    var creatorProfile = await _dbContext.Profiles
                        .FirstOrDefaultAsync(p => p.UserId == escrowTx.CreatorUserId, cancellationToken);

                    if (creatorProfile != null)
                    {
                        var creatorWallet = await _dbContext.Set<EscrowWallet>()
                            .FirstOrDefaultAsync(ew => ew.ProfileId == creatorProfile.Id, cancellationToken);
                        if (creatorWallet != null)
                        {
                            creatorWallet.LockedBalance -= creatorAmount;
                            creatorWallet.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE \"AspNetUsers\" SET \"EscrowBalance\" = GREATEST(\"EscrowBalance\" - {0}, 0) WHERE \"Id\" = {1}",
                        new object[] { creatorAmount, escrowTx.CreatorUserId }, cancellationToken);
                }
            }

            // Only THIS item is refunded — set its own status. Sibling items keep theirs.
            orderItem.OrderItemStatus = OrderStatusEnum.Refunded;
            orderItem.EscrowStatus = EscrowStatusEnum.Refunded;
            orderItem.UpdatedAt = DateTime.UtcNow;

            // Only mark the whole order as Refunded once every active item is refunded.
            // On a multi-item order, approving a single item must not flag the entire order,
            // otherwise the remaining items appear already resolved and can't be approved.
            var allOrderItems = await _dbContext.OrderProductAffiliates
                .Where(opa => opa.OrderId == order.Id && opa.IsActive)
                .ToListAsync(cancellationToken);

            // orderItem's EscrowStatus is set above but not yet saved; it is the same tracked
            // instance EF returns here, so its new value is reflected in the All(...) check.
            var allRefunded = allOrderItems.All(opa =>
                opa.OrderItemStatus == OrderStatusEnum.Refunded ||
                opa.EscrowStatus == EscrowStatusEnum.Refunded);

            if (allRefunded)
            {
                order.OrderStatus = OrderStatusEnum.Refunded;
                order.UpdatedAt = DateTime.UtcNow;
            }

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                ProfileId = sellerProfile.Id,
                TransactionType = TransactionTypeEnum.Refund,
                Amount = -refundAmount,
                CurrencyId = order.CurrencyId,
                OrderId = order.Id,
                OrderProductAffiliateId = orderItem.Id,
                Description = order.Uid,
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatusEnum.Completed
            });

            _dbContext.WalletTransactions.Add(new WalletTransaction
            {
                ProfileId = order.ProfileId,
                TransactionType = TransactionTypeEnum.Refund,
                Amount = refundAmount,
                CurrencyId = order.CurrencyId,
                OrderId = order.Id,
                OrderProductAffiliateId = orderItem.Id,
                Description = order.Uid,
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatusEnum.Completed
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            var buyerProfile = await _dbContext.Profiles.FirstOrDefaultAsync(
                p => p.Id == order.ProfileId, cancellationToken);

            if (buyerProfile != null)
            {
                await _notificationService.SaveRefundApprovedNotificationAsync(
                    sellerProfile.Id,
                    buyerProfile.Id,
                    orderItem.Uid);
            }

            await _emailService.SendRefundResolvedToBuyerAsync(
                order,
                orderItem,
                true,
                "Seller approved the refund.");

            return new RefundApproveResponse
            {
                Success = true,
                Message = $"Refund of {order.Currency?.Code ?? "AED"} {refundAmount:F2} has been approved and initiated to the buyer's original payment method. " +
                          "Note: Stripe processing fees are non-refundable. The buyer will receive the net order amount.",
                ProductOrderUid = orderItem.Uid,
                RefundAmount = refundAmount,
                StripeRefundId = stripeRefund.RefundId,
                EscrowStatus = orderItem.EscrowStatus.ToString()
            };
        }
    }
}
