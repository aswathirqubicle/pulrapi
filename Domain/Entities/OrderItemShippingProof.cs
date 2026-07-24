using System;

namespace Core.Domain.Entities;

public class OrderItemShippingProof : EntityBase
{
    public int OrderProductAffiliateId { get; set; }
    public OrderProductAffiliate OrderProductAffiliate { get; set; }

    public int MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; }

    public int Priority { get; set; } = 0;
}