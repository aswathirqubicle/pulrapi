namespace Core.Domain.Entities;

public class ProductSubCategoryLevel2 : EntityBase
{
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int SubCategoryLevel2Id { get; set; }
    public SubCategoryLevel2 SubCategoryLevel2 { get; set; }
}