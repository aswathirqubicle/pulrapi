using Core.Domain.Enums;
using System;

namespace Core.Application.Models.Users
{
    public class NotificationDto
    {
        public string Uid { get; set; }
        public string ActorProfileId { get; set; }
        public string ActorName { get; set; }
        public string ActorAvatar { get; set; }
        public string ActionType { get; set; }
        public string ReceiverUserId { get; set; }
        public string ReceiverName { get; set; }
        public string PostId { get; set; }
        public string PostImageUrl { get; set; }
        public string StoryImageUrl { get; set; }
        public string ProductImageUrl { get; set; }
        public bool ReceriverFollweredByActor { get; set; }
        public bool CanFollowBack { get; set; } // True when the actor follows receiver but receiver doesn't follow back (for follow back button)
        public EntityTypeEnum TargetType { get; set; } // Post, Comment etc.
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Value { get; set; }
        public string RequesterProfileType { get; set; } // "public" or "private" - for follow request notifications
        public string Title { get; set; }
        public string Body { get; set; }
        public int? FollowerCount { get; set; }
    }
} 