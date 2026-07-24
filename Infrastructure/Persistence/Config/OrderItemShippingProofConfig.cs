using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config;

public class OrderItemShippingProofConfig : IEntityTypeConfiguration<OrderItemShippingProof>
{
    public void Configure(EntityTypeBuilder<OrderItemShippingProof> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasOne(sp => sp.OrderProductAffiliate)
            .WithMany(opa => opa.ShippingProofs)
            .HasForeignKey(sp => sp.OrderProductAffiliateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.MediaFile)
            .WithMany()
            .HasForeignKey(sp => sp.MediaFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}