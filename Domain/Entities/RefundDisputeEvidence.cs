using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class RefundDisputeEvidence : EntityBase
{
    [Required]
    public int RefundDisputeId { get; set; }
    public RefundDispute RefundDispute { get; set; }

    [Required]
    public int MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; }

    [Required]
    public EvidenceTypeEnum EvidenceType { get; set; } = EvidenceTypeEnum.Other;

    public int Priority { get; set; } = 0;
}