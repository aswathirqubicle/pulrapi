using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class RefundDispute : EntityBase
    {
        [Required]
        public int OrderProductAffiliateId { get; set; }
        public OrderProductAffiliate OrderProductAffiliate { get; set; }

        public int? SellerProfileId { get; set; }
        public Profile? SellerProfile { get; set; }

        public int? BuyerProfileId { get; set; }
        public Profile? BuyerProfile { get; set; }

        [Required]
        public DisputeStatusEnum Status { get; set; } = DisputeStatusEnum.Pending;

        [MaxLength(2000)]
        public string? SellerRejectionReason { get; set; }
        public DateTime? SellerRejectedAt { get; set; }

        [MaxLength(2000)]
        public string? AdminResolutionNotes { get; set; }
        public DateTime? AdminResolvedAt { get; set; }
        public string? ResolvedByAdminUserId { get; set; }

        public virtual ICollection<RefundDisputeEvidence> EvidenceFiles { get; set; }

        [MaxLength(1000)]
        public string BuyerRefundReason { get; set; }
        public DateTime? BuyerRefundRequestedAt { get; set; }

        [MaxLength(200)]
        public string ReturnFullName { get; set; }
        [MaxLength(500)]
        public string ReturnAddressLine1 { get; set; }
        [MaxLength(500)]
        public string ReturnAddressLine2 { get; set; }
        [MaxLength(200)]
        public string ReturnCity { get; set; }
        [MaxLength(200)]
        public string ReturnState { get; set; }
        [MaxLength(50)]
        public string ReturnPostalCode { get; set; }
        [MaxLength(200)]
        public string ReturnCountry { get; set; }
        [MaxLength(50)]
        public string ReturnPhone { get; set; }
    }
}
