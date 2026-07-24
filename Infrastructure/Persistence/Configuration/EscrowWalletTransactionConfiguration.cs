using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class EscrowWalletTransactionConfiguration : IEntityTypeConfiguration<EscrowWalletTransaction>
    {
        public void Configure(EntityTypeBuilder<EscrowWalletTransaction> builder)
        {
            builder.ToTable("EscrowWalletTransactions");

            builder.HasOne(ewt => ewt.EscrowWallet)
                .WithMany(ew => ew.EscrowWalletTransactions)
                .HasForeignKey(ewt => ewt.EscrowWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ewt => ewt.Order)
                .WithMany()
                .HasForeignKey(ewt => ewt.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ewt => ewt.OrderProductAffiliate)
                .WithMany()
                .HasForeignKey(ewt => ewt.OrderProductAffiliateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(ewt => ewt.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Property(ewt => ewt.SellerAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(ewt => ewt.CreatorAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(ewt => ewt.PlatformAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(ewt => ewt.StripePaymentIntentId)
                .HasMaxLength(255);

            builder.Property(ewt => ewt.StripeTransferIdSeller)
                .HasMaxLength(255);

            builder.Property(ewt => ewt.StripeTransferIdCreator)
                .HasMaxLength(255);

            builder.Property(ewt => ewt.StripeRefundId)
                .HasMaxLength(255);

            builder.HasIndex(ewt => ewt.StripePaymentIntentId);
        }
    }
}