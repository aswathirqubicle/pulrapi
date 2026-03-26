using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class AffiliateConfig : IEntityTypeConfiguration<Affiliate>
    {
        public void Configure(EntityTypeBuilder<Affiliate> builder)
        {

            builder.HasIndex(e => e.AffiliateId)
                   .IsUnique();
        }
    }
}
