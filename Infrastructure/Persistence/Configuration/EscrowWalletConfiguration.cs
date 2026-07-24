using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class EscrowWalletConfiguration : IEntityTypeConfiguration<EscrowWallet>
    {
        public void Configure(EntityTypeBuilder<EscrowWallet> builder)
        {
            builder.ToTable("EscrowWallets");

            builder.HasOne(ew => ew.Profile)
                .WithMany()
                .HasForeignKey(ew => ew.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ew => ew.Currency)
                .WithMany()
                .HasForeignKey(ew => ew.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(ew => ew.LockedBalance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);
        }
    }
}