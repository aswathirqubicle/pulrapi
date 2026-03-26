using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProfileFollower : EntityBase
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        [Required]
        public int FollowerId { get; set; }
        public Profile Follower { get; set; }
    }
}
