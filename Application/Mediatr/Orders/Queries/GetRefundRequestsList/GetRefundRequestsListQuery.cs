using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Core.Application.Mediatr.Orders.Queries.GetRefundRequestsList
{
    public class GetRefundRequestsListQuery : IRequest<List<RefundRequestSummaryDto>>
    {
    }

    public class RefundRequestSummaryDto
    {
        public string DisputeUid { get; set; }
        public string OrderUid { get; set; }
        public string ProductOrderUid { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string ProductBrand { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductDescription { get; set; }
        public List<string> VariantTypes { get; set; }
        public string BuyerName { get; set; }
        public string Status { get; set; }
        public DateTime RequestedOn { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class GetRefundRequestsListQueryHandler : IRequestHandler<GetRefundRequestsListQuery, List<RefundRequestSummaryDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetRefundRequestsListQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<RefundRequestSummaryDto>> Handle(GetRefundRequestsListQuery request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            var disputes = await _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Order)
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Product)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Where(rd => rd.BuyerProfileId == profile.Id || rd.SellerProfileId == profile.Id)
                .OrderByDescending(rd => rd.CreatedAt)
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

                return new RefundRequestSummaryDto
                {
                    DisputeUid = dispute.Uid,
                    OrderUid = orderItem?.Order?.Uid ?? string.Empty,
                    ProductOrderUid = orderItem?.Uid ?? string.Empty,
                    ProductName = orderItem?.ProductNameSnapshot ?? string.Empty,
                    ProductImageUrl = productImageUrl ?? string.Empty,
                    ProductBrand = orderItem?.ProductBrandSnapshot ?? string.Empty,
                    UnitPrice = orderItem?.ProductPriceSnapshot ?? 0,
                    ProductDescription = orderItem?.ProductDescriptionSnapshot ?? string.Empty,
                    VariantTypes = !string.IsNullOrWhiteSpace(orderItem?.VariantTypesSnapshot)
                        ? JsonConvert.DeserializeObject<List<string>>(orderItem.VariantTypesSnapshot) ?? new List<string>()
                        : new List<string>(),
                    BuyerName = GetUserDisplayName(dispute.BuyerProfile?.User),
                    Status = dispute.Status.ToString(),
                    RequestedOn = dispute.CreatedAt,
                    RefundAmount = refundAmount
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