using System.Collections.Generic;

namespace Core.Domain.Entities;

public class SubCategoryLevel2 : EntityBase
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public int SubCategoryLevel1Id { get; set; }
    public SubCategoryLevel1 SubCategoryLevel1 { get; set; }
    public virtual ICollection<ProductSubCategoryLevel2> ProductSubCategoryLevel2 { get; set; } =
        new List<ProductSubCategoryLevel2>();
}