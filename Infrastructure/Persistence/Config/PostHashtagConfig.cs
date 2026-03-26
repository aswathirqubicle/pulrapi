using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostHashtagConfig : IEntityTypeConfiguration<PostHashtag>
    {
        public void Configure(EntityTypeBuilder<PostHashtag> builder)
        {

            builder.HasKey(pf => new { pf.PostId, pf.HashtagId });

            builder.HasOne(vsp => vsp.Post)
                .WithMany(spo => spo.PostHashtags)
                .HasForeignKey(pf => pf.PostId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
