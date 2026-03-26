using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Persistence.Config;

//public class ProductSubCategoryLevel2Config : IEntityTypeConfiguration<ProductSubCategoryLevel2>
//{
//    public void Configure(EntityTypeBuilder<ProductSubCategoryLevel2> builder)
//    {
//        builder.HasOne(e => e.Product)
//            .WithMany(p => p.ProductSubCategoryLevel2)
//            .HasForeignKey(e => e.ProductId);

//        builder.HasOne(e => e.SubCategoryLevel2)
//            .WithMany(p => p.ProductSubCategoryLevel2)
//            .HasForeignKey(e => e.SubCategoryLevel2Id);
//    }
//}