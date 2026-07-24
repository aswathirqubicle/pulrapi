using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class RefundDisputeEvidenceConfiguration : IEntityTypeConfiguration<RefundDisputeEvidence>
    {
        public void Configure(EntityTypeBuilder<RefundDisputeEvidence> builder)
        {
            builder.ToTable("RefundDisputeEvidences");

            builder.HasOne(rde => rde.RefundDispute)
                .WithMany(rd => rd.EvidenceFiles)
                .HasForeignKey(rde => rde.RefundDisputeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rde => rde.MediaFile)
                .WithMany()
                .HasForeignKey(rde => rde.MediaFileId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}