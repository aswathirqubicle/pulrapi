using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config;

public class OrderProductAffiliateConfig : IEntityTypeConfiguration<OrderProductAffiliate>
{
    public void Configure(EntityTypeBuilder<OrderProductAffiliate> builder)
    {
        // Use the surrogate Id key from EntityBase as the primary key.
        // This allows multiple line items for the same product (e.g., different variants) in the same order.
        builder.HasKey(b => b.Id);

        builder.HasOne(o => o.Order)
            .WithMany(o => o.OrderProductAffiliates)
            .HasForeignKey(b => b.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(o => o.Product)
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        // Affiliate is optional - make relationship optional
        builder.HasOne(o => o.Affiliate)
            .WithOne(a => a.OrderProductAffiliate)
            .HasForeignKey<OrderProductAffiliate>(b => b.AffiliateId)
            .IsRequired(false) // Make Affiliate optional
            .OnDelete(DeleteBehavior.NoAction);

        //builder.HasOne(o => o.Product)
        //    .WithOne(p => p.OrderProductAffiliate)
        //    .HasForeignKey<OrderProductAffiliate>(b => b.ProductId)
        //    .OnDelete(DeleteBehavior.NoAction);

        // Composite index for the countdown background job query:
        // WHERE OrderItemStatus = 'Processing' AND CountdownExpiryDate < now
        builder.HasIndex(e => new { e.OrderItemStatus, e.CountdownExpiryDate })
               .HasDatabaseName("IX_OrderProductAffiliates_Status_Countdown");
    }
}
