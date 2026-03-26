using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config;

public class CategoryClosureConfig : IEntityTypeConfiguration<CategoryClosure>
{
    public void Configure(EntityTypeBuilder<CategoryClosure> builder)
    {
        builder.HasOne(cc => cc.Ancestor)
            .WithMany()
            .HasForeignKey(cc => cc.AncestorId)
            .OnDelete(DeleteBehavior.Restrict); 

        builder.HasOne(cc => cc.Descendant)
            .WithMany()
            .HasForeignKey(cc => cc.DescendantId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}