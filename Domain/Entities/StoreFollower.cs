
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class StoreFollower
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int StoreId { get; set; }
        public Store Store { get; set; }
        [Required]
        public int FollowerId { get; set; }
        public Profile Follower { get; set; }
    }
}
