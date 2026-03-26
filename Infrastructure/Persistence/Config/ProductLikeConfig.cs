using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductLikeConfig : IEntityTypeConfiguration<ProductLike>
    {
        public void Configure(EntityTypeBuilder<ProductLike> builder)
        {
            builder.HasOne(pl => pl.Product)
                .WithMany()
                .HasForeignKey(pl => pl.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.LikedBy)
                .WithMany()
                .HasForeignKey(pl => pl.LikedById)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
