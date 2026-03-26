using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProfileFollowerConfig : IEntityTypeConfiguration<ProfileFollower>
    {
        public void Configure(EntityTypeBuilder<ProfileFollower> builder)
        {

            builder.HasKey(pf => new { pf.ProfileId, pf.FollowerId });

            builder.HasOne(vsp => vsp.Profile)
                .WithMany(spo => spo.ProfileFollowers)
                .HasForeignKey(pf => pf.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(vsp => vsp.Follower)
                .WithMany(spo => spo.ProfileFollowings)
                .HasForeignKey(pf => pf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
