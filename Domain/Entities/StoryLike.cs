namespace Core.Domain.Entities;

public class StoryLike : EntityBase
{
    public int StoryId { get; set; }
    public Story Story { get; set; }
    public int LikedById { get; set; }
    public Profile LikedBy { get; set; }
}