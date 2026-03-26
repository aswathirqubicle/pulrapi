using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class FulfillmentConfig : IEntityTypeConfiguration<Fulfillment>
    {
        public void Configure(EntityTypeBuilder<Fulfillment> builder)
        {
            builder.Property(o => o.FulfillmentMethod).HasConversion<string>();
        }
    }
}
