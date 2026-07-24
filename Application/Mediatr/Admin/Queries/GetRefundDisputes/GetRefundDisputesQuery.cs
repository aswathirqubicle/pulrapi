using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Admin.Queries.GetRefundDisputes
{
    public class GetRefundDisputesQuery : IRequest<List<RefundDisputeSummaryDto>>
    {
        public DisputeStatusEnum? Status { get; set; }
    }

    public class RefundDisputeSummaryDto
    {
        public string Uid { get; set; }
        public string OrderProductAffiliateUid { get; set; }
        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public DisputeStatusEnum Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SellerRejectedAt { get; set; }
        public int EvidenceFileCount { get; set; }
    }

    public class GetRefundDisputesQueryHandler : IRequestHandler<GetRefundDisputesQuery, List<RefundDisputeSummaryDto>>
    {
        private readonly IApplicationDbContext _dbContext;

        public GetRefundDisputesQueryHandler(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RefundDisputeSummaryDto>> Handle(GetRefundDisputesQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Include(rd => rd.SellerProfile)
                    .ThenInclude(sp => sp.User)
                .Include(rd => rd.EvidenceFiles)
                .AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(rd => rd.Status == request.Status.Value);
            }

            var disputes = await query
                .OrderByDescending(rd => rd.CreatedAt)
                .ToListAsync(cancellationToken);

            return disputes.Select(rd => new RefundDisputeSummaryDto
            {
                Uid = rd.Uid,
                OrderProductAffiliateUid = rd.OrderProductAffiliate?.Uid ?? string.Empty,
                BuyerName = GetUserDisplayName(rd.BuyerProfile?.User),
                SellerName = GetUserDisplayName(rd.SellerProfile?.User),
                Status = rd.Status,
                CreatedAt = rd.CreatedAt,
                SellerRejectedAt = rd.SellerRejectedAt,
                EvidenceFileCount = rd.EvidenceFiles?.Count ?? 0
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
