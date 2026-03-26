using System.Collections.Generic;

namespace Core.Application.Mediatr.Categories.Queries;

public class CategoryResponse
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public List<CategoryResponse> SubCategories { get; set; } = new List<CategoryResponse>();
    public string ParentCategoryUid { get; set; }
}


