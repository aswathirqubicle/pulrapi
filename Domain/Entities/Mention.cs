using Core.Domain.Common;
using Core.Domain.Enums;
using System;

namespace Core.Domain.Entities
{
    public class Mention : EntityBase
    {
        public int MentionedUserId { get; set; }
        public int MentionedByUserId { get; set; }
        public string TargetId { get; set; } // PostId or CommentId
        public EntityTypeEnum MentionType { get; set; } // Post or Comment
    }
} 