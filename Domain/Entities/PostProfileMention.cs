using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostProfileMention
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
    }
}
