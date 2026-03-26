using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config
{
    public class BookmarkCollectionItemConfig : IEntityTypeConfiguration<BookmarkCollectionItem>
    {
        public void Configure(EntityTypeBuilder<BookmarkCollectionItem> builder)
        {
            builder.HasKey(bci => bci.Id);

            builder.HasOne(bci => bci.Post)
                .WithMany(p => p.BookmarkCollectionItems)
                .HasForeignKey(bci => bci.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(bci => bci.BookmarkCollection)
                .WithMany(bc => bc.BookmarkCollectionItems)
                .HasForeignKey(bci => bci.BookmarkCollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(bci => bci.PostId);
            builder.HasIndex(bci => bci.BookmarkCollectionId);
        }
    }
}
