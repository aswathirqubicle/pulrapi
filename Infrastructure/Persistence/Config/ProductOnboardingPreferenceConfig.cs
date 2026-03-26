using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    //internal class ProductOnboardingPreferenceConfig : IEntityTypeConfiguration<ProductOnboardingPreference>
    //{
    //    public void Configure(EntityTypeBuilder<ProductOnboardingPreference> builder)
    //    {

    //        builder.HasKey(e => new { e.ProductId, e.OnboardingPreferenceId });

    //        builder.HasOne(e => e.Product)
    //            .WithMany(p => p.ProductOnboardingPreferences)
    //            .HasForeignKey(pf => pf.ProductId)
    //            .OnDelete(DeleteBehavior.NoAction);
    //    }
    //}
}
