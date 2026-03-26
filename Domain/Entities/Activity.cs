using Core.Domain.Common;
using Core.Domain.Enums;
using System;

namespace Core.Domain.Entities
{
    public class Activity : EntityBase
    {
        public string UserId { get; set; }
        public ActivityActionTypeEnum ActionType { get; set; }
        public string TargetId { get; set; } // Can be PostId, CommentId etc.
        public EntityTypeEnum TargetType { get; set; } // Post, Comment etc.
    }
} 