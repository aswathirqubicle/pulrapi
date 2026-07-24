using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Stripe;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Admin.Commands.ResolveRefundDispute
{
    public class ResolveRefundDisputeCommand : IRequest<ResolveRefundDisputeResponse>
    {
        [Required]
        public string DisputeUid { get; set; }

        [Required]
        public string Decision { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        public decimal? NetRefundAmount { get; set; }
    }

    public class ResolveRefundDisputeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string DisputeUid { get; set; }
        public string Decision { get; set; }
        public string StripeRefundId { get; set; }
    }

    public class ResolveRefundDisputeCommandHandler : IRequestHandler<ResolveRefundDisputeCommand, ResolveRefundDisputeResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IStripeService _stripeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResolveRefundDisputeCommandHandler> _logger;

        public ResolveRefundDisputeCommandHandler(
            IApplicationDbContext dbContext,
            IStripeService stripeService,
            ICurrentUserService currentUserService,
            ISettingsCacheService settingsCacheService,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<ResolveRefundDisputeCommandHandler> logger)
        {
            _dbContext = dbContext;
            _stripeService = stripeService;
            _currentUserService = currentUserService;
            _settingsCacheService = settingsCacheService;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ResolveRefundDisputeResponse> Handle(ResolveRefundDisputeCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            if (!_currentUserService.HasRole(PulrRoles.Administrator) && !_currentUserService.HasRole(PulrRoles.Moderator))
            {
                throw new ForbiddenException("Only administrators or moderators can resolve refund disputes.");
            }

            var dispute = await _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Order)
                        .ThenInclude(o => o.Currency)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Include(rd => rd.SellerProfile)
                    .ThenInclude(sp => sp.User)
                .FirstOrDefaultAsync(
                    rd => rd.Uid == request.DisputeUid && rd.Status == DisputeStatusEnum.UnderReview,
                    cancellationToken);

            if (dispute == null)
            {
                throw new NotFoundException($"Refund dispute with UID {request.DisputeUid} not found or is not under review.");
            }

            var orderItem = dispute.OrderProductAffiliate;
            var order = orderItem?.Order;
            var isApproved = request.Decision?.ToLowerInvariant() == "approve";

            string stripeRefundId = null;

            if (isApproved)
            {
                if (order == null)
                {
                    throw new NotFoundException("Order not found for this dispute.");
                }

                if (string.IsNullOrEmpty(order.StripePaymentIntentId))
                {
                    throw new BadRequestException("No Stripe payment intent found for this order. Cannot process refund.");
                }

                decimal refundAmount;
                long refundAmountInCents;

                if (request.NetRefundAmount.HasValue && request.NetRefundAmount.Value > 0)
                {
                    refundAmount = request.NetRefundAmount.Value;
                    refundAmountInCents = (long)Math.Round(refundAmount * 100, MidpointRounding.AwayFromZero);
                }
                else
                {
                    refundAmount = order.Amount;
                    refundAmountInCents = (long)Math.Round(refundAmount * 100, MidpointRounding.AwayFromZero);
                }

                if (refundAmountInCents <= 0)
                {
                    throw new BadRequestException("Refund amount must be greater than zero.");
                }

                _logger.LogInformation(
                    "Admin {UserId} processing Stripe refund for dispute {DisputeUid}, PaymentIntent: {PaymentIntentId}, Amount: {Amount} cents",
                    user.Id, dispute.Uid, order.StripePaymentIntentId, refundAmountInCents);

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
                            { "refund_type", "admin_approved" },
                            { "dispute_uid", dispute.Uid },
                            { "net_amount", refundAmount.ToString("F2") }
                        }
                    };

                    var stripeRefund = await _stripeService.CreateRefundAsync(refundRequest);
                    stripeRefundId = stripeRefund.RefundId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe refund failed for dispute {DisputeUid}, PaymentIntent: {PaymentIntentId}",
                        dispute.Uid, order.StripePaymentIntentId);
                    throw new BadRequestException($"Stripe refund failed: {ex.Message}. Please try again later.");
                }

                orderItem.EscrowStatus = EscrowStatusEnum.Refunded;
                orderItem.UpdatedAt = DateTime.UtcNow;

                order.OrderStatus = OrderStatusEnum.Refunded;
                order.UpdatedAt = DateTime.UtcNow;

                var escrowTx = await _dbContext.Set<EscrowWalletTransaction>()
                    .FirstOrDefaultAsync(
                        ewt => ewt.OrderProductAffiliateId == orderItem.Id
                            && ewt.Status == EscrowWalletTransactionStatusEnum.Active,
                        cancellationToken);

                if (escrowTx != null)
                {
                    escrowTx.Status = EscrowWalletTransactionStatusEnum.Refunded;
                    escrowTx.StripeRefundId = stripeRefundId;
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

                        // Mirror onto the AspNetUsers.EscrowBalance rollup (not mapped in EF here; raw SQL).
                        if (sellerLocked > 0m)
                        {
                            await _dbContext.Database.ExecuteSqlRawAsync(
                                "UPDATE \"AspNetUsers\" SET \"EscrowBalance\" = GREATEST(\"EscrowBalance\" - {0}, 0) WHERE \"Id\" = (SELECT \"UserId\" FROM \"Profiles\" WHERE \"Id\" = {1})",
                                new object[] { sellerLocked, escrowWallet.ProfileId }, cancellationToken);
                        }
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

                if (dispute.SellerProfileId.HasValue)
                {
                    _dbContext.WalletTransactions.Add(new WalletTransaction
                    {
                        ProfileId = dispute.SellerProfileId.Value,
                        TransactionType = TransactionTypeEnum.Refund,
                        Amount = -refundAmount,
                        CurrencyId = order.CurrencyId,
                        OrderId = order.Id,
                        OrderProductAffiliateId = orderItem.Id,
                        Description = order.Uid,
                        TransactionDate = DateTime.UtcNow,
                        Status = TransactionStatusEnum.Completed
                    });
                }

                var refundBuyerProfileId = dispute.BuyerProfileId ?? order.ProfileId;
                _dbContext.WalletTransactions.Add(new WalletTransaction
                {
                    ProfileId = refundBuyerProfileId,
                    TransactionType = TransactionTypeEnum.Refund,
                    Amount = refundAmount,
                    CurrencyId = order.CurrencyId,
                    OrderId = order.Id,
                    OrderProductAffiliateId = orderItem.Id,
                    Description = order.Uid,
                    TransactionDate = DateTime.UtcNow,
                    Status = TransactionStatusEnum.Completed
                });
            }
            else
            {
                orderItem.EscrowStatus = EscrowStatusEnum.Released;
                orderItem.UpdatedAt = DateTime.UtcNow;
            }

            dispute.Status = DisputeStatusEnum.Resolved;
            dispute.AdminResolutionNotes = request.Notes;
            dispute.AdminResolvedAt = DateTime.UtcNow;
            dispute.ResolvedByAdminUserId = user.Id;
            dispute.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            int adminProfileId = 0;
            int buyerProfileId = 0;
            int? sellerProfileId = null;

            var adminProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (adminProfile != null)
            {
                adminProfileId = adminProfile.Id;
            }

            if (dispute.BuyerProfileId.HasValue)
            {
                buyerProfileId = dispute.BuyerProfileId.Value;
            }

            if (dispute.SellerProfileId.HasValue)
            {
                sellerProfileId = dispute.SellerProfileId.Value;
            }

            await _notificationService.SaveRefundResolvedNotificationAsync(
                adminProfileId,
                buyerProfileId,
                sellerProfileId,
                orderItem.Uid);

            await _emailService.SendRefundResolvedToBuyerAsync(
                order,
                orderItem,
                isApproved,
                request.Notes);

            _logger.LogInformation(
                "Admin {UserId} resolved refund dispute {DisputeUid} with decision: {Decision}",
                user.Id, dispute.Uid, request.Decision);

            return new ResolveRefundDisputeResponse
            {
                Success = true,
                Message = isApproved
                    ? "Refund has been approved and initiated to the buyer's original payment method."
                    : "Refund request has been rejected.",
                DisputeUid = dispute.Uid,
                Decision = request.Decision,
                StripeRefundId = stripeRefundId
            };
        }
    }
}
