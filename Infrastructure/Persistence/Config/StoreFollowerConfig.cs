using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class StoreFollowerConfig : IEntityTypeConfiguration<StoreFollower>
    {
        public void Configure(EntityTypeBuilder<StoreFollower> builder)
        {

            builder.HasKey(pf => new { pf.StoreId, pf.FollowerId });

            builder.HasOne(vsp => vsp.Store)
                .WithMany(spo => spo.StoreFollowers)
                .HasForeignKey(pf => pf.StoreId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
