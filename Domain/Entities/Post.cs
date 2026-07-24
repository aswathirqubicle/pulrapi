using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Post : EntityBase
    {
        public string Text { get; set; }
        public string ImgDescription { get; set; }
        public string Location { get; set; }
		public int? ImageWidth { get; set; }
		public int? ImageHeight { get; set; }
		public int? VideoWidth { get; set; }
		public int? VideoHeight { get; set; }
        public DateTime? DeletedAt { get; set; }

        [Required]
        public User User { get; set; }

        public Store Store { get; set; }
        [Required]
        public MediaFile MediaFile { get; set; }
        public int? SharedPostId { get; set; }
        public Post SharedPost { get; set; }

        public string ThumbnailUrl { get; set; }
        public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
        public virtual ICollection<PostProfileMention> PostProfileMentions { get; set; } = new List<PostProfileMention>();
        public virtual ICollection<PostStoreMention> PostStoreMentions { get; set; } = new List<PostStoreMention>();
        public virtual ICollection<PostProductTag> PostProductTags { get; set; } = new List<PostProductTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<BookmarkCollectionItem> BookmarkCollectionItems { get; set; } = new List<BookmarkCollectionItem>();
        public virtual ICollection<PostClick> PostClicks { get; set; } = new List<PostClick>();
        public virtual ICollection<PostMyStyle> PostMyStyles { get; set; } = new List<PostMyStyle>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
        public string? CollabId { get; set; }
    }
}