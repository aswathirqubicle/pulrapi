using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config
{
    public class ShippingDetailsConfig : IEntityTypeConfiguration<ShippingDetails>
    {
        public void Configure(EntityTypeBuilder<ShippingDetails> builder)
        {
            // Explicitly configure the ONE relationship between User and ShippingDetails
            // so EF does not try to infer extra shadow FKs like UserId1/UserId2/UserId3.
            builder.HasOne(sd => sd.User)
                .WithMany(u => u.ShippingDetails)
                .HasForeignKey(sd => sd.UserId).IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(sd => sd.CountryNavigation)
                .WithMany()
                .HasForeignKey(sd => sd.CountryUid)
                .HasPrincipalKey(c => c.Uid)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}



