using Core.Domain.Enums;

namespace Core.Application.Models.Post
{
    public class TaggedUserResponse
    {
        public string ProfileUid { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool FollowedByMe { get; set; }
        public string UserType { get; set; }
    }
} 