using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Configuration
{
    public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(EntityTypeBuilder<UserBlock> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.BlockerProfile)
                .WithMany(x => x.BlockedUsers)
                .HasForeignKey(x => x.BlockerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.BlockedProfile)
                .WithMany(x => x.BlockedByUsers)
                .HasForeignKey(x => x.BlockedProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Uid);
        }
    }
} 