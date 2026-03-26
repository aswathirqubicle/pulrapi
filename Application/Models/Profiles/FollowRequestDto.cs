using System;

namespace Core.Application.Models.Profiles
{
    public class FollowRequestDto
    {
        public string Uid { get; set; }
        public string RequesterProfileUid { get; set; }
        public string RequesterName { get; set; }
        public string RequesterAvatar { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}

