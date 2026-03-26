using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductVariantCombinationConfig : IEntityTypeConfiguration<ProductVariantCombination>
    {
        public void Configure(EntityTypeBuilder<ProductVariantCombination> builder)
        {
            builder.HasOne(pvc => pvc.Product)
                .WithMany(p => p.ProductVariantCombinations)
                .HasForeignKey(pvc => pvc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(pvc => pvc.CombinationOptions)
                .WithOne(pco => pco.ProductVariantCombination)
                .HasForeignKey(pco => pco.ProductVariantCombinationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure SKU is unique
            builder.HasIndex(pvc => pvc.SKU)
                .IsUnique();

            builder.Property(pvc => pvc.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
