using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class UserWishlistProductConfig : IEntityTypeConfiguration<UserWishlistProduct>
    {
        public void Configure(EntityTypeBuilder<UserWishlistProduct> builder)
        {
            // Composite key: UserId, WishlistProductId, and optionally ProductVariantCombinationUid
            // Note: Since ProductVariantCombinationUid can be null, we use Id as primary key
            // but add unique index for the combination
            builder.HasIndex(w => new { w.UserId, w.WishlistProductId, w.ProductVariantCombinationUid })
                .IsUnique()
                .HasFilter("\"ProductVariantCombinationUid\" IS NOT NULL");

            builder.HasIndex(w => new { w.UserId, w.WishlistProductId })
                .IsUnique()
                .HasFilter("\"ProductVariantCombinationUid\" IS NULL");

            builder.HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(w => w.WishlistProduct)
                .WithMany()
                .HasForeignKey(w => w.WishlistProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.ProductVariantCombination)
                .WithMany()
                .HasForeignKey(w => w.ProductVariantCombinationUid)
                .HasPrincipalKey(pvc => pvc.Uid)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

