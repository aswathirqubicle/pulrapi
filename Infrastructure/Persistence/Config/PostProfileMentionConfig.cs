using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostProfileMentionConfig : IEntityTypeConfiguration<PostProfileMention>
    {
        public void Configure(EntityTypeBuilder<PostProfileMention> builder)
        {

            builder.HasKey(pf => new { pf.PostId, pf.ProfileId });

            builder.HasOne(vsp => vsp.Post)
                .WithMany(spo => spo.PostProfileMentions)
                .HasForeignKey(pf => pf.PostId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
