using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Configure relationship with ShippingDetails (required)
            builder.HasOne(o => o.ShippingDetails)
                .WithMany()
                .HasForeignKey(o => o.ShippingDetailsId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with BillingDetails (optional)
            // If BillingDetailsId is null, use ShippingDetails for billing
            builder.HasOne(o => o.BillingDetails)
                .WithMany()
                .HasForeignKey(o => o.BillingDetailsId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
