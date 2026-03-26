using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostLike : EntityBase
    {
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int LikedById { get; set; }
        public Profile LikedBy { get; set; }
    }
}
