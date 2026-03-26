using System.Collections.Generic;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Categories.Queries;

public class CategoryTree
{
    public CategoryTree(Category category, int numLevel)
    {
        Category = category;
        NumLevel = numLevel;
        SubCategories = new List<CategoryTree>();
    }

    public CategoryTree(Category category, int numLevel, List<CategoryTree> subCategories)
        : this(category, numLevel)
    {
        SubCategories.AddRange(subCategories);
    }

    public Category Category { get; }
    public int NumLevel { get; }
    public List<CategoryTree> SubCategories { get; }
}