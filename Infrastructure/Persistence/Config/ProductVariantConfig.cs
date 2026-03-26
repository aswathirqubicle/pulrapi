using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariant)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pv => pv.ProductVariantOptions)
                .WithOne(pvo => pvo.ProductVariant)
                .HasForeignKey(pvo => pvo.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 