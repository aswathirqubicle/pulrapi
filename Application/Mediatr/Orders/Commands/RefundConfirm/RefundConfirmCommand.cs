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

namespace Core.Application.Mediatr.Orders.Commands.RefundConfirm
{
    public class RefundConfirmCommand : IRequest<RefundConfirmResponse>
    {
        [Required]
        public string OrderProductAffiliateUid { get; set; }
    }

    public class RefundConfirmResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string OrderProductAffiliateUid { get; set; }
        public decimal RefundAmount { get; set; }
        public string StripeRefundId { get; set; }
        public string EscrowStatus { get; set; }
    }

    public class RefundConfirmCommandHandler : IRequestHandler<RefundConfirmCommand, RefundConfirmResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStripeService _stripeService;
        private readonly ILogger<RefundConfirmCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISettingsCacheService _settingsCacheService;

        public RefundConfirmCommandHandler(
            IApplicationDbContext dbContext,
            IStripeService stripeService,
            ILogger<RefundConfirmCommandHandler> logger,
            ICurrentUserService currentUserService,
            ISettingsCacheService settingsCacheService)
        {
            _dbContext = dbContext;
            _stripeService = stripeService;
            _logger = logger;
            _currentUserService = currentUserService;
            _settingsCacheService = settingsCacheService;
        }

        public async Task<RefundConfirmResponse> Handle(RefundConfirmCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var orderItem = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Order)
                    .ThenInclude(o => o.Currency)
                .Include(opa => opa.Product)
                    .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(opa => opa.Uid == request.OrderProductAffiliateUid && opa.IsActive, cancellationToken);

            if (orderItem == null)
            {
                throw new NotFoundException($"Order item with UID {request.OrderProductAffiliateUid} not found.");
            }

            var order = orderItem.Order;
            var store = orderItem.Product?.Store;

            if (store == null)
            {
                throw new BadRequestException("Store information not found for this order item.");
            }

            var sellerProfile = await _dbContext.Profiles.FirstOrDefaultAsync(
                p => p.UserId == store.UserId, cancellationToken);

            if (sellerProfile == null || sellerProfile.UserId != user.Id)
            {
                throw new ForbiddenException("Only the seller can confirm a refund return receipt.");
            }

            if (orderItem.EscrowStatus != EscrowStatusEnum.RefundInProgress)
            {
                throw new BadRequestException($"Cannot confirm refund. Current escrow status: {orderItem.EscrowStatus}. Expected: RefundInProgress.");
            }

            if (string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                throw new BadRequestException("No Stripe payment intent found for this order. Cannot process refund.");
            }

            long refundAmountInCents;
            decimal refundAmount;

            refundAmount = order.Amount;
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
                        { "refund_type", "buyer_initiated_return" },
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
                    if (sellerLocked > 0m && store.UserId != null)
                    {
                        await _dbContext.Database.ExecuteSqlRawAsync(
                            "UPDATE \"AspNetUsers\" SET \"EscrowBalance\" = GREATEST(\"EscrowBalance\" - {0}, 0) WHERE \"Id\" = {1}",
                            new object[] { sellerLocked, store.UserId }, cancellationToken);
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

                // Only THIS item is refunded — set its own status. Sibling items keep
                // their own statuses (Delivered / etc.).
                orderItem.OrderItemStatus = OrderStatusEnum.Refunded;
                orderItem.EscrowStatus = EscrowStatusEnum.Refunded;
                orderItem.UpdatedAt = DateTime.UtcNow;

                // Close the whole order only once every active item is refunded.
                var allOrderItems = await _dbContext.OrderProductAffiliates
                    .Where(opa => opa.OrderId == order.Id && opa.IsActive)
                    .ToListAsync(cancellationToken);

                var allRefunded = allOrderItems.All(opa =>
                    opa.OrderItemStatus == OrderStatusEnum.Refunded ||
                    opa.EscrowStatus == EscrowStatusEnum.Refunded);

                if (allRefunded)
                {
                    order.OrderStatus = OrderStatusEnum.Refunded;
                }
                order.UpdatedAt = DateTime.UtcNow;

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

                await _dbContext.SaveChangesAsync(cancellationToken);

            return new RefundConfirmResponse
            {
                Success = true,
                Message = $"Refund of {order.Currency?.Code ?? "AED"} {refundAmount:F2} has been initiated to the buyer's original payment method. " +
                          "Note: Stripe processing fees are non-refundable. The buyer will receive the net order amount.",
                OrderProductAffiliateUid = orderItem.Uid,
                RefundAmount = refundAmount,
                StripeRefundId = stripeRefund.RefundId,
                EscrowStatus = orderItem.EscrowStatus.ToString()
            };
        }
    }
}