using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class UserBagProductConfig : IEntityTypeConfiguration<UserBagProduct>
    {
        public void Configure(EntityTypeBuilder<UserBagProduct> builder)
        {
            // Use Id as primary key since variant combination can be null
            // Add unique index for the combination
            builder.HasIndex(b => new { b.UserId, b.BagProductId, b.ProductVariantCombinationUid })
                .IsUnique()
                .HasFilter("\"ProductVariantCombinationUid\" IS NOT NULL");

            builder.HasIndex(b => new { b.UserId, b.BagProductId })
                .IsUnique()
                .HasFilter("\"ProductVariantCombinationUid\" IS NULL");

            builder.HasOne(b => b.User)
                .WithMany(u => u.BagItems)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(b => b.BagProduct)
                .WithMany()
                .HasForeignKey(b => b.BagProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.ProductVariantCombination)
                .WithMany()
                .HasForeignKey(b => b.ProductVariantCombinationUid)
                .HasPrincipalKey(pvc => pvc.Uid)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
