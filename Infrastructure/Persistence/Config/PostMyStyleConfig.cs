using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostMyStyleConfig : IEntityTypeConfiguration<PostMyStyle>
    {
        public void Configure(EntityTypeBuilder<PostMyStyle> builder)
        {
            builder.HasKey(e => new { e.PostId, e.ProfileId });

            builder.HasOne(vsp => vsp.Profile)
                .WithMany(spo => spo.PostMyStyles)
                .HasForeignKey(pf => pf.ProfileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
