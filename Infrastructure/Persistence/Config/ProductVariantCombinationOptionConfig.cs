using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProductVariantCombinationOptionConfig : IEntityTypeConfiguration<ProductVariantCombinationOption>
    {
        public void Configure(EntityTypeBuilder<ProductVariantCombinationOption> builder)
        {
            builder.HasOne(pvco => pvco.ProductVariantCombination)
                .WithMany(pvc => pvc.CombinationOptions)
                .HasForeignKey(pvco => pvco.ProductVariantCombinationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pvco => pvco.ProductVariantOption)
                .WithMany()
                .HasForeignKey(pvco => pvco.ProductVariantOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure a combination doesn't have duplicate option values
            builder.HasIndex(pvco => new { pvco.ProductVariantCombinationId, pvco.ProductVariantOptionId })
                .IsUnique();
        }
    }
}
