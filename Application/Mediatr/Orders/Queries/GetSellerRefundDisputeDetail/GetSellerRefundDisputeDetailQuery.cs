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

namespace Core.Application.Mediatr.Orders.Queries.GetSellerRefundDisputeDetail
{
    public class GetSellerRefundDisputeDetailQuery : IRequest<SellerRefundDisputeDetailDto>
    {
        [Required]
        public string DisputeUid { get; set; }
    }

    public class SellerRefundDisputeDetailDto
    {
        public string Uid { get; set; }
        public string OrderProductAffiliateUid { get; set; }
        public string OrderNumber { get; set; }
        public string ProductName { get; set; }
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

    public class GetSellerRefundDisputeDetailQueryHandler : IRequestHandler<GetSellerRefundDisputeDetailQuery, SellerRefundDisputeDetailDto>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetSellerRefundDisputeDetailQueryHandler(
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<SellerRefundDisputeDetailDto> Handle(GetSellerRefundDisputeDetailQuery request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            var dispute = await _dbContext.RefundDisputes
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Order)
                .Include(rd => rd.OrderProductAffiliate)
                    .ThenInclude(opa => opa.Product)
                        .ThenInclude(p => p.Store)
                            .ThenInclude(s => s.User)
                .Include(rd => rd.BuyerProfile)
                    .ThenInclude(bp => bp.User)
                .Include(rd => rd.EvidenceFiles)
                    .ThenInclude(ef => ef.MediaFile)
                .FirstOrDefaultAsync(rd => rd.Uid == request.DisputeUid, cancellationToken);

            if (dispute == null)
                throw new NotFoundException($"Refund dispute with UID {request.DisputeUid} not found.");

            var orderItem = dispute.OrderProductAffiliate;
            var storeSellerUserId = orderItem?.Product?.Store?.User?.Id;

            if (storeSellerUserId != user.Id)
                throw new ForbiddenException("You are not authorized to view this refund request.");

            var order = orderItem?.Order;
            var refundAmount = (orderItem?.ProductPriceSnapshot ?? 0) * (orderItem?.ProductQuantity ?? 0);

            return new SellerRefundDisputeDetailDto
            {
                Uid = dispute.Uid,
                OrderProductAffiliateUid = orderItem?.Uid ?? string.Empty,
                OrderNumber = order?.Uid ?? string.Empty,
                ProductName = orderItem?.ProductNameSnapshot ?? string.Empty,
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