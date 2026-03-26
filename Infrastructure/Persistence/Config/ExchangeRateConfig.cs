using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ExchangeRateConfig : IEntityTypeConfiguration<ExchangeRate>
    {
        public void Configure(EntityTypeBuilder<ExchangeRate> builder)
        {
            builder.HasOne(vsp => vsp.GlobalCurrencySetting)
                .WithMany(spo => spo.ExchangeRates)
                .HasForeignKey(pf => pf.GlobalCurrencySettingId);
        }
    }
}
