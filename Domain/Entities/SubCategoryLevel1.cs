using System.Collections.Generic;

namespace Core.Domain.Entities;

public class SubCategoryLevel1 : EntityBase
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public List<SubCategoryLevel2> SubCategoriesLevel2s { get; set; } = new List<SubCategoryLevel2>();
}