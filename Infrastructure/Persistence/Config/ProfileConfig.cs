using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProfileConfig : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Uid).IsUnique();

            builder.HasMany(x => x.BlockedUsers)
                .WithOne(x => x.BlockerProfile)
                .HasForeignKey(x => x.BlockerProfileId)
                .HasPrincipalKey(x => x.Uid)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.BlockedByUsers)
                .WithOne(x => x.BlockedProfile)
                .HasForeignKey(x => x.BlockedProfileId)
                .HasPrincipalKey(x => x.Uid)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
