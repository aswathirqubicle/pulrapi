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

namespace Core.Application.Mediatr.Orders.Queries.GetOrderRefundRequests
{
    public class GetOrderRefundRequestsQuery : IRequest<List<OrderRefundRequestDto>>
    {
        [Required]
        public string OrderUid { get; set; }
    }

    public class OrderRefundRequestDto
    {
        public string Uid { get; set; }
        public string ProductOrderUid { get; set; }
        public string OrderNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string BuyerName { get; set; }
        public string BuyerRefundReason { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal RefundAmount { get; set; }
        public ReturnAddressDto ReturnAddress { get; set; }
        public List<EvidenceFileDto> EvidenceFiles { get; set; }
    }

    public class ReturnAddressDto
    {
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
    }

    public class EvidenceFileDto
    {
        public string Uid { get; set; }
        public string Url { get; set; }
        public string MediaType { get; set; }
        public EvidenceTypeEnum EvidenceType { get; set; }
    }

    public class GetOrderRefundRequestsQueryHandler : IRequestHandler<GetOrderRefundRequestsQuery, List<OrderRefundRequestDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetOrderRefundRequestsQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<OrderRefundRequestDto>> Handle(GetOrderRefundRequestsQuery request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            // The UID may be either a full order UID or a sub-order (per-item) UID.
            // When a sub-order UID is passed, we resolve its parent order and return only
            // that item's refund request; otherwise all items in the order are returned.
            int? targetItemId = null;

            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid, cancellationToken);

            if (order == null)
            {
                var subOrderItem = await _dbContext.OrderProductAffiliates
                    .Include(opa => opa.Order)
                    .FirstOrDefaultAsync(opa => opa.Uid == request.OrderUid, cancellationToken);

                if (subOrderItem?.Order != null)
                {
                    order = subOrderItem.Order;
                    targetItemId = subOrderItem.Id;
                }
            }

            if (order == null)
                throw new NotFoundException($"Order with UID {request.OrderUid} not found.");

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            var isBuyer = profile != null && order.ProfileId == profile.Id;
            var isSeller = !isBuyer && await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Product)
                .AnyAsync(opa => opa.OrderId == order.Id && opa.Product.UserId == user.Id, cancellationToken);

            if (!isBuyer && !isSeller)
                throw new ForbiddenException("You are not authorized to view refund requests for this order.");

            var disputes = await _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Product)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Include(rd => rd.EvidenceFiles)
                    .ThenInclude(ef => ef.MediaFile)
                .Where(rd => rd.OrderProductAffiliate.OrderId == order.Id
                    && (targetItemId == null || rd.OrderProductAffiliateId == targetItemId.Value))
                .ToListAsync(cancellationToken);

            return disputes.Select(dispute =>
            {
                var orderItem = dispute.OrderProductAffiliate;
                var refundAmount = (orderItem?.ProductPriceSnapshot ?? 0) * (orderItem?.ProductQuantity ?? 0);

                // Prefer the snapshot captured at order time; fall back to the live product's
                // primary image when the snapshot is empty (e.g. older orders predating snapshots).
                var productImageUrl = orderItem?.PrimaryImageUrlSnapshot;
                if (string.IsNullOrWhiteSpace(productImageUrl))
                {
                    productImageUrl = orderItem?.Product?.ProductMediaFiles?
                        .Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive)
                        .OrderBy(pmf => pmf.MediaFile.Priority)
                        .FirstOrDefault()?.MediaFile?.Url;
                }

                return new OrderRefundRequestDto
                {
                    Uid = dispute.Uid,
                    ProductOrderUid = orderItem?.Uid ?? string.Empty,
                    OrderNumber = order.Uid,
                    ProductName = orderItem?.ProductNameSnapshot ?? string.Empty,
                    ProductImageUrl = productImageUrl ?? string.Empty,
                    BuyerName = GetUserDisplayName(dispute.BuyerProfile?.User),
                    BuyerRefundReason = dispute.BuyerRefundReason ?? string.Empty,
                    Status = dispute.Status.ToString(),
                    CreatedAt = dispute.CreatedAt,
                    RefundAmount = refundAmount,
                    ReturnAddress = new ReturnAddressDto
                    {
                        FullName = dispute.ReturnFullName ?? string.Empty,
                        AddressLine1 = dispute.ReturnAddressLine1 ?? string.Empty,
                        AddressLine2 = dispute.ReturnAddressLine2 ?? string.Empty,
                        City = dispute.ReturnCity ?? string.Empty,
                        State = dispute.ReturnState ?? string.Empty,
                        PostalCode = dispute.ReturnPostalCode ?? string.Empty,
                        Country = dispute.ReturnCountry ?? string.Empty,
                        Phone = dispute.ReturnPhone ?? string.Empty
                    },
                    EvidenceFiles = dispute.EvidenceFiles?.Select(ef => new EvidenceFileDto
                    {
                        Uid = ef.Uid,
                        Url = ef.MediaFile?.Url ?? string.Empty,
                        MediaType = ef.MediaFile?.MediaFileType.ToString() ?? string.Empty,
                        EvidenceType = ef.EvidenceType
                    }).ToList() ?? new List<EvidenceFileDto>()
                };
            }).ToList();
        }

        private static string GetUserDisplayName(User user)
        {
            if (user == null) return string.Empty;
            if (!string.IsNullOrEmpty(user.DisplayName)) return user.DisplayName;
            if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
                return $"{user.FirstName} {user.LastName}".Trim();
            return user.UserName ?? string.Empty;
        }
    }
}