using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class CommentLikeConfig : IEntityTypeConfiguration<CommentLike>
    {
        public void Configure(EntityTypeBuilder<CommentLike> builder)
        {

            builder.HasKey(pf => new { pf.CommentId, pf.LikedById });

            builder.HasOne(vsp => vsp.Comment)
                .WithMany(spo => spo.CommentLikes)
                .HasForeignKey(pf => pf.CommentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
