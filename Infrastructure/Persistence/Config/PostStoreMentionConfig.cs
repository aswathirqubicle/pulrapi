using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    public class PostStoreMentionConfig : IEntityTypeConfiguration<PostStoreMention>
    {
        public void Configure(EntityTypeBuilder<PostStoreMention> builder)
        {

            builder.HasKey(pf => new { pf.PostId, pf.StoreId });

            builder.HasOne(vsp => vsp.Post)
                .WithMany(spo => spo.PostStoreMentions)
                .HasForeignKey(pf => pf.PostId)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
