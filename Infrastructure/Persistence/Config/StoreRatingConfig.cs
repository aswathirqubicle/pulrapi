using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class StoreRatingConfig : IEntityTypeConfiguration<StoreRating>
    {
        public void Configure(EntityTypeBuilder<StoreRating> builder)
        {

            builder.HasKey(e => new { e.StoreId, e.RatedById });

            builder.HasOne(e => e.Store)
                .WithMany(e => e.StoreRatings)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
