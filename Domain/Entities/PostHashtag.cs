using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostHashtag
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int HashtagId { get; set; }
        public Hashtag Hashtag { get; set; }
    }
}
