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

namespace Core.Application.Mediatr.Admin.Queries.GetRefundDisputeDetail
{
    public class GetRefundDisputeDetailQuery : IRequest<RefundDisputeDetailDto>
    {
        [Required]
        public string DisputeUid { get; set; }
    }

    public class RefundDisputeDetailDto
    {
        public string Uid { get; set; }
        public string OrderProductAffiliateUid { get; set; }
        public DisputeStatusEnum Status { get; set; }
        public string SellerRejectionReason { get; set; }
        public DateTime? SellerRejectedAt { get; set; }
        public string AdminResolutionNotes { get; set; }
        public DateTime? AdminResolvedAt { get; set; }
        public string OrderNumber { get; set; }
        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public string ProductName { get; set; }
        public decimal RefundAmount { get; set; }
        public List<EvidenceFileDto> EvidenceFiles { get; set; }
    }

    public class EvidenceFileDto
    {
        public string Uid { get; set; }
        public string Url { get; set; }
        public string MediaType { get; set; }
        public EvidenceTypeEnum EvidenceType { get; set; }
    }

    public class GetRefundDisputeDetailQueryHandler : IRequestHandler<GetRefundDisputeDetailQuery, RefundDisputeDetailDto>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetRefundDisputeDetailQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RefundDisputeDetailDto> Handle(GetRefundDisputeDetailQuery request, CancellationToken cancellationToken)
        {
            var dispute = await _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Order)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Include(rd => rd.SellerProfile)
                    .ThenInclude(sp => sp.User)
                .Include(rd => rd.EvidenceFiles)
                    .ThenInclude(ef => ef.MediaFile)
                .FirstOrDefaultAsync(rd => rd.Uid == request.DisputeUid, cancellationToken);

            if (dispute == null)
            {
                throw new NotFoundException($"Refund dispute with UID {request.DisputeUid} not found.");
            }

            var orderItem = dispute.OrderProductAffiliate;
            var order = orderItem?.Order;

            var refundAmount = (orderItem?.ProductPriceSnapshot ?? 0) * (orderItem?.ProductQuantity ?? 0);

            return new RefundDisputeDetailDto
            {
                Uid = dispute.Uid,
                OrderProductAffiliateUid = orderItem?.Uid ?? string.Empty,
                Status = dispute.Status,
                SellerRejectionReason = dispute.SellerRejectionReason ?? string.Empty,
                SellerRejectedAt = dispute.SellerRejectedAt,
                AdminResolutionNotes = dispute.AdminResolutionNotes ?? string.Empty,
                AdminResolvedAt = dispute.AdminResolvedAt,
                OrderNumber = order?.Uid ?? string.Empty,
                BuyerName = GetUserDisplayName(dispute.BuyerProfile?.User),
                SellerName = GetUserDisplayName(dispute.SellerProfile?.User),
                ProductName = orderItem?.ProductNameSnapshot ?? string.Empty,
                RefundAmount = refundAmount,
                EvidenceFiles = dispute.EvidenceFiles?.Select(ef => new EvidenceFileDto
                {
                    Uid = ef.Uid,
                    Url = ef.MediaFile?.Url ?? string.Empty,
                    MediaType = ef.MediaFile?.MediaFileType.ToString() ?? string.Empty,
                    EvidenceType = ef.EvidenceType
                }).ToList() ?? new List<EvidenceFileDto>()
            };
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
