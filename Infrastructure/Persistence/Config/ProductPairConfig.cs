using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;

namespace Core.Infrastructure.Persistence.Config
{
    //internal class ProductPairConfig : IEntityTypeConfiguration<ProductPair>
    //{
    //    public void Configure(EntityTypeBuilder<ProductPair> builder)
    //    {

    //        builder.HasKey(e => new { e.ProductId, e.PairId });

    //        builder.HasOne(e => e.Product)
    //            .WithMany(p => p.ProductPairs)
    //            .HasForeignKey(pf => pf.ProductId)
    //            .OnDelete(DeleteBehavior.NoAction);
    //    }
    //}
}
