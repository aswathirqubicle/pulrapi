using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostLikeConfig : IEntityTypeConfiguration<PostLike>
    {
        public void Configure(EntityTypeBuilder<PostLike> builder)
        {

            builder.HasKey(pf => new { pf.PostId, pf.LikedById });

            builder.HasOne(vsp => vsp.Post)
                .WithMany(spo => spo.PostLikes)
                .HasForeignKey(pf => pf.PostId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
