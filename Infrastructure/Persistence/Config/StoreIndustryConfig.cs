using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class StoreIndustryConfig : IEntityTypeConfiguration<StoreIndustry>
    {
        public void Configure(EntityTypeBuilder<StoreIndustry> builder)
        {

            builder.HasKey(pf => new { pf.StoreId, pf.IndustryId });

            builder.HasOne(vsp => vsp.Store)
                .WithMany(spo => spo.StoreIndustries)
                .HasForeignKey(pf => pf.StoreId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
