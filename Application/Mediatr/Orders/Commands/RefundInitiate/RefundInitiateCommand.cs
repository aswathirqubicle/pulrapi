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

namespace Core.Application.Mediatr.Orders.Commands.RefundInitiate
{
    public class RefundInitiateCommand : IRequest<RefundInitiateResponse>
    {
        [Required]
        public string OrderUid { get; set; }

        [Required]
        public string ShippingDetailsUid { get; set; }

        [Required]
        [MinLength(1)]
        public List<RefundLineItemRequest> LineItems { get; set; } = new();
    }

    public class RefundLineItemRequest
    {
        [Required]
        public string ProductOrderUid { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; }

        public List<string> EvidenceFileUids { get; set; } = new List<string>();
    }

    public class RefundInitiateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<RefundLineItemResult> Results { get; set; } = new();
    }

    public class RefundLineItemResult
    {
        public string ProductOrderUid { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal? NetRefundAmount { get; set; }
        public string DisputeUid { get; set; }
    }

    public class RefundInitiateCommandHandler : IRequestHandler<RefundInitiateCommand, RefundInitiateResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<RefundInitiateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISettingsCacheService _settingsCacheService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public RefundInitiateCommandHandler(
            IApplicationDbContext dbContext,
            ILogger<RefundInitiateCommandHandler> logger,
            ICurrentUserService currentUserService,
            ISettingsCacheService settingsCacheService,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _settingsCacheService = settingsCacheService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<RefundInitiateResponse> Handle(RefundInitiateCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (profile == null)
                throw new NotFoundException("Profile not found.");

            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid, cancellationToken);

            if (order == null)
                throw new NotFoundException($"Order with UID {request.OrderUid} not found.");

            if (order.ProfileId != profile.Id)
                throw new ForbiddenException("You are not authorized to request a refund for this order.");

            var shippingDetails = await _dbContext.ShippingDetails
                .Include(sd => sd.CountryNavigation)
                .FirstOrDefaultAsync(sd => sd.Uid == request.ShippingDetailsUid, cancellationToken);

            if (shippingDetails == null)
                throw new NotFoundException("Shipping address not found.");

            var productOrderUids = request.LineItems.Select(li => li.ProductOrderUid).ToList();

            var orderItems = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Product)
                .Where(opa => opa.OrderId == order.Id && productOrderUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            var results = new List<RefundLineItemResult>();
            var successfulDisputes = new List<(RefundDispute Dispute, OrderProductAffiliate Item, decimal NetAmount)>();

            foreach (var lineItemReq in request.LineItems)
            {
                var orderItem = orderItems.FirstOrDefault(opa => opa.Uid == lineItemReq.ProductOrderUid);

                if (orderItem == null)
                {
                    results.Add(new RefundLineItemResult
                    {
                        ProductOrderUid = lineItemReq.ProductOrderUid,
                        Success = false,
                        Message = $"Order item with UID {lineItemReq.ProductOrderUid} not found."
                    });
                    continue;
                }

                if (orderItem.EscrowStatus != EscrowStatusEnum.InEscrow && orderItem.EscrowStatus != EscrowStatusEnum.PendingDelivery)
                {
                    results.Add(new RefundLineItemResult
                    {
                        ProductOrderUid = lineItemReq.ProductOrderUid,
                        Success = false,
                        Message = $"Cannot request refund. Current escrow status: {orderItem.EscrowStatus}."
                    });
                    continue;
                }

                if (orderItem.DeliveredAt == null)
                {
                    results.Add(new RefundLineItemResult
                    {
                        ProductOrderUid = lineItemReq.ProductOrderUid,
                        Success = false,
                        Message = "Cannot request refund for an item that has not been delivered yet."
                    });
                    continue;
                }

                var platformSettings = await _settingsCacheService.GetPlatformSettingsAsync();
                var refundWindowDays = platformSettings?.RefundWindowDays ?? 3;
                var refundEligibleUntil = orderItem.RefundEligibleUntil ?? orderItem.DeliveredAt.Value.AddDays(refundWindowDays);

                if (DateTime.UtcNow > refundEligibleUntil)
                {
                    results.Add(new RefundLineItemResult
                    {
                        ProductOrderUid = lineItemReq.ProductOrderUid,
                        Success = false,
                        Message = $"Refund window has expired. Items can only be refunded within {refundWindowDays} days of delivery."
                    });
                    continue;
                }

                if (lineItemReq.EvidenceFileUids.Count > 3)
                {
                    results.Add(new RefundLineItemResult
                    {
                        ProductOrderUid = lineItemReq.ProductOrderUid,
                        Success = false,
                        Message = "Maximum 3 evidence files allowed per product."
                    });
                    continue;
                }

                var sellerProfileId = orderItem.Product?.UserId != null
                    ? (await _dbContext.Profiles.FirstOrDefaultAsync(
                        p => p.UserId == orderItem.Product.UserId, cancellationToken))?.Id
                    : null;

                var refundDispute = new RefundDispute
                {
                    OrderProductAffiliateId = orderItem.Id,
                    BuyerProfileId = profile.Id,
                    SellerProfileId = sellerProfileId,
                    Status = DisputeStatusEnum.Pending,
                    BuyerRefundReason = lineItemReq.Reason,
                    BuyerRefundRequestedAt = DateTime.UtcNow,
                    ReturnFullName = $"{shippingDetails.FirstName} {shippingDetails.LastName}".Trim(),
                    ReturnAddressLine1 = shippingDetails.Address,
                    ReturnAddressLine2 = !string.IsNullOrWhiteSpace(shippingDetails.Floor)
                        ? shippingDetails.Floor
                        : shippingDetails.Apartment,
                    ReturnCity = shippingDetails.City,
                    ReturnState = shippingDetails.Region,
                    ReturnPostalCode = shippingDetails.ZipCode,
                    ReturnCountry = shippingDetails.CountryNavigation?.Name,
                    ReturnPhone = shippingDetails.PhoneNumber
                };
                _dbContext.RefundDisputes.Add(refundDispute);

                if (lineItemReq.EvidenceFileUids.Any())
                {
                    var mediaFiles = await _dbContext.MediaFiles
                        .Where(mf => lineItemReq.EvidenceFileUids.Contains(mf.Uid))
                        .ToListAsync(cancellationToken);

                    refundDispute.EvidenceFiles ??= new List<RefundDisputeEvidence>();

                    for (int i = 0; i < mediaFiles.Count; i++)
                    {
                        refundDispute.EvidenceFiles.Add(new RefundDisputeEvidence
                        {
                            MediaFileId = mediaFiles[i].Id,
                            EvidenceType = EvidenceTypeEnum.BuyerEvidence,
                            Priority = i
                        });
                    }
                }

                orderItem.EscrowStatus = EscrowStatusEnum.RefundRequested;
                orderItem.UpdatedAt = DateTime.UtcNow;

                var netAmount = (orderItem.ProductPriceSnapshot ?? 0) * orderItem.ProductQuantity;
                successfulDisputes.Add((refundDispute, orderItem, netAmount));

                results.Add(new RefundLineItemResult
                {
                    ProductOrderUid = lineItemReq.ProductOrderUid,
                    Success = true,
                    Message = "Refund request sent. Waiting for seller approval.",
                    NetRefundAmount = netAmount,
                    DisputeUid = refundDispute.Uid
                });
            }

            if (!successfulDisputes.Any())
            {
                return new RefundInitiateResponse
                {
                    Success = false,
                    Message = "All refund requests failed.",
                    Results = results
                };
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var sellerGroups = successfulDisputes
                .Where(sd => sd.Dispute.SellerProfileId.HasValue)
                .GroupBy(sd => sd.Dispute.SellerProfileId.Value)
                .ToList();

            foreach (var sellerGroup in sellerGroups)
            {
                var firstItem = sellerGroup.First();
                await _notificationService.SaveRefundRequestNotificationAsync(
                    profile.Id, sellerGroup.Key, firstItem.Item.Uid);

                await _emailService.SendRefundRequestToSellerAsync(
                    order, firstItem.Item, firstItem.NetAmount);
            }

            _logger.LogInformation("Buyer {UserId} requested refund for {Count} item(s) in order {OrderUid}. {SuccessCount} succeeded, {FailCount} failed.",
                user.Id, request.LineItems.Count, request.OrderUid, successfulDisputes.Count, results.Count(r => !r.Success));

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);
            var message = failCount > 0
                ? $"{successCount} of {request.LineItems.Count} refund requests submitted. {failCount} failed."
                : $"{successCount} refund request(s) submitted. Waiting for seller approval.";

            return new RefundInitiateResponse
            {
                Success = successCount > 0,
                Message = message,
                Results = results
            };
        }
    }
}