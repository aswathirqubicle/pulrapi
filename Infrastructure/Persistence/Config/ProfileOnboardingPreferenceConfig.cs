using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class ProfileOnboardingPreferenceConfig : IEntityTypeConfiguration<ProfileOnboardingPreference>
    {
        public void Configure(EntityTypeBuilder<ProfileOnboardingPreference> builder)
        {
            builder.HasKey(pop => new { pop.ProfileId, pop.OnboardingPreferenceId });

            builder.HasOne(p => p.Profile)
                .WithMany(p => p.ProfileOnboardingPreferences)
                .HasForeignKey(pf => pf.ProfileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}