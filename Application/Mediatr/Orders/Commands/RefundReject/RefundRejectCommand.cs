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
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Commands.RefundReject
{
    public class RefundRejectCommand : IRequest<RefundRejectResponse>
    {
        [Required]
        public string OrderUid { get; set; }

        [Required]
        public string ProductOrderUid { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; }

        public List<string> MediaFileUids { get; set; } = new List<string>();
    }

    public class RefundRejectResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ProductOrderUid { get; set; }
        public string DisputeUid { get; set; }
        public List<string> EvidenceFileUrls { get; set; }
    }

    public class RefundRejectCommandHandler : IRequestHandler<RefundRejectCommand, RefundRejectResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<RefundRejectCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public RefundRejectCommandHandler(
            IApplicationDbContext dbContext,
            ILogger<RefundRejectCommandHandler> logger,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<RefundRejectResponse> Handle(RefundRejectCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                throw new NotAuthenticatedException("User must be logged in.");
            }

            var order = await _dbContext.Orders
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
                throw new ForbiddenException("Only the seller can reject a refund request.");
            }

            if (orderItem.EscrowStatus != EscrowStatusEnum.RefundRequested)
            {
                throw new BadRequestException($"Cannot reject refund. Current escrow status: {orderItem.EscrowStatus}. Expected: RefundRequested.");
            }

            var refundDispute = await _dbContext.RefundDisputes
                .FirstOrDefaultAsync(
                    rd => rd.OrderProductAffiliateId == orderItem.Id
                        && rd.Status == DisputeStatusEnum.Pending,
                    cancellationToken);

            if (refundDispute == null)
            {
                throw new NotFoundException("Pending refund dispute not found for this order item.");
            }

            // Update RefundDispute with seller rejection details
            refundDispute.Status = DisputeStatusEnum.UnderReview;
            refundDispute.SellerRejectionReason = request.Reason;
            refundDispute.SellerRejectedAt = DateTime.UtcNow;
            refundDispute.SellerProfileId = sellerProfile.Id;

            // Add evidence files if provided
            var evidenceUrls = new List<string>();
            if (request.MediaFileUids != null && request.MediaFileUids.Any())
            {
                for (int i = 0; i < request.MediaFileUids.Count; i++)
                {
                    var mediaFileUid = request.MediaFileUids[i];
                    var mediaFile = await _dbContext.MediaFiles
                        .FirstOrDefaultAsync(mf => mf.Uid == mediaFileUid, cancellationToken);

                    if (mediaFile == null)
                    {
                        throw new NotFoundException($"Media file with UID {mediaFileUid} not found.");
                    }

                    var evidence = new RefundDisputeEvidence
                    {
                        RefundDisputeId = refundDispute.Id,
                        MediaFileId = mediaFile.Id,
                        EvidenceType = EvidenceTypeEnum.RejectionReason,
                        Priority = i
                    };

                    _dbContext.RefundDisputeEvidences.Add(evidence);
                    evidenceUrls.Add(mediaFile.Url);
                }
            }

            // Update order item escrow status
            orderItem.EscrowStatus = EscrowStatusEnum.RefundRejected;
            orderItem.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Notifications and emails
            var buyerProfile = await _dbContext.Profiles.FirstOrDefaultAsync(
                p => p.Id == order.ProfileId, cancellationToken);

            if (buyerProfile != null)
            {
                await _notificationService.SaveRefundRejectedNotificationAsync(
                    sellerProfile.Id,
                    buyerProfile.Id,
                    orderItem.Uid);

                await _emailService.SendRefundRejectedToBuyerAsync(order, orderItem, request.Reason);

                await _notificationService.SaveRefundDisputedNotificationAsync(
                    buyerProfile.Id,
                    sellerProfile.Id,
                    orderItem.Uid);
            }

            await _emailService.SendRefundDisputedToAdminAsync(refundDispute);

            _logger.LogInformation(
                "Seller {UserId} rejected refund for order item {ItemUid}. EscrowStatus set to RefundRejected.",
                user.Id,
                orderItem.Uid);

            return new RefundRejectResponse
            {
                Success = true,
                Message = "Refund request has been rejected and the dispute is now under review.",
                ProductOrderUid = orderItem.Uid,
                DisputeUid = refundDispute.Uid,
                EvidenceFileUrls = evidenceUrls
            };
        }
    }
}
