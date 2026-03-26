namespace Core.Application.Mediatr.Stories.Queries;

public class StoryToggleLikeResponse
{
    public bool LikedByMe { get; set; }
    public int LikesCount { get; set; }
}