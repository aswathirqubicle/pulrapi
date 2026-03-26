using Core.Domain.Common;
using Core.Domain.Enums;
using System;

namespace Core.Domain.Entities
{
    public class NotificationHistory : EntityBase
    {
        public int ReceiverUserId { get; set; }
        public int ActorUserId { get; set; }
        public NotificationActionTypeEnum ActionType { get; set; }
        public EntityTypeEnum TargetType { get; set; } // Post, Comment, etc.
        public string TargetId { get; set; }
        public bool IsRead { get; set; } = false;
        public string CommentText { get; set; }
        public string RequesterProfileType { get; set; } // "public" or "private" - stores the requester's profile type for follow requests

        // Navigation properties
        public virtual Profile ActorProfile { get; set; }
        public virtual Profile ReceiverProfile { get; set; }

        // batchCount (unread notification count) is sent in push notification data, not stored here
    }
} 