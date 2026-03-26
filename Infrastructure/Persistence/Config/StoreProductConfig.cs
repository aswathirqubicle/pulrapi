using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class StoreProductConfig : IEntityTypeConfiguration<StoreProduct>
    {
        public void Configure(EntityTypeBuilder<StoreProduct> builder)
        {

            builder.HasKey(e => new { e.StoreId, e.ProductId });

            builder.HasOne(e => e.Store)
                .WithMany(e => e.StoreProducts)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
