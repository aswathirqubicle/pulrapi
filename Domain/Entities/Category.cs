using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities;

public class Category : EntityBase
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string Slug { get; set; }

    public int? ParentCategoryId { get; set; }
    public Category ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; }

    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}