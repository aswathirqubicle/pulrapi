using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Comment : EntityBase
    {
        [Required]
        public string Content { get; set; }

        [Required]
        public Profile CommentedBy { get; set; }

        public Post Post { get; set; }
        public int? ProductId { get; set; }
        public Product Product { get; set; }
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

        // Add parent-child relationship for replies
        public int? ParentCommentId { get; set; }
        public Comment ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}