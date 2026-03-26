using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    //internal class ProductSimilarConfig : IEntityTypeConfiguration<ProductSimilar>
    //{
    //    public void Configure(EntityTypeBuilder<ProductSimilar> builder)
    //    {

    //        builder.HasKey(e => new { e.ProductId, e.SimilarId });

    //        builder.HasOne(e => e.Product)
    //            .WithMany(p => p.ProductSimilars)
    //            .HasForeignKey(pf => pf.ProductId)
    //            .OnDelete(DeleteBehavior.NoAction);
    //    }
    //}
}
