using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class PostClick : EntityBase
    {
        [Required]
        public Post Post { get; set; }
        public int Count { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
