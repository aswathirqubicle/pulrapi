using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class RefundDisputeConfiguration : IEntityTypeConfiguration<RefundDispute>
    {
        public void Configure(EntityTypeBuilder<RefundDispute> builder)
        {
            builder.ToTable("RefundDisputes");

            builder.HasOne(rd => rd.OrderProductAffiliate)
                .WithMany()
                .HasForeignKey(rd => rd.OrderProductAffiliateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rd => rd.SellerProfile)
                .WithMany()
                .HasForeignKey(rd => rd.SellerProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rd => rd.BuyerProfile)
                .WithMany()
                .HasForeignKey(rd => rd.BuyerProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(rd => new { rd.Status });
            builder.HasIndex(rd => new { rd.OrderProductAffiliateId });
        }
    }
}