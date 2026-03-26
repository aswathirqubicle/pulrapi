namespace Core.Domain.Entities;

public class CategoryClosure : EntityBase
{
    public int AncestorId { get; set; }
    public Category Ancestor { get; set; }
    public int DescendantId { get; set; }
    public Category Descendant { get; set; }
    public int NumLevel { get; set; }
}