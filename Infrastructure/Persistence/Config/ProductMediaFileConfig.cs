using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductMediaFileConfig : IEntityTypeConfiguration<ProductMediaFile>
    {
        public void Configure(EntityTypeBuilder<ProductMediaFile> builder)
        {
            builder.HasOne(pmf => pmf.Product)
                .WithMany(p => p.ProductMediaFiles)
                .HasForeignKey(pmf => pmf.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pmf => pmf.MediaFile)
                .WithMany()
                .HasForeignKey(pmf => pmf.MediaFileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
