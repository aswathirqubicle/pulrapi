using System.ComponentModel.DataAnnotations;
using System;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostMyStyle : EntityBase
    {
        [Required]
        public int PostId { get; set; }
        public Post Post { get; set; }
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
    }
}
