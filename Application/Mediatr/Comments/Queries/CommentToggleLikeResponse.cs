namespace Core.Application.Mediatr.Comments.Queries;

public class CommentToggleLikeResponse
{
    public int LikesCount { get; set; }
    public bool LikedByMe { get; set; }
}