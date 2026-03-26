namespace Core.Domain.Entities;

public class StoryProductTag : EntityBase
{
    public int StoryId { get; set; }
    public Story Story { get; set; }
    public string AffiliateId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public double PositionLeftPercent { get; set; }
    public double PositionTopPercent { get; set; }
}