namespace Core.Application.Models.Post
{
    public class ToggleLikePostDto
    {
        public bool LikedByMe { get; set; }
        public int LikesCount { get; set; }
    }
}
