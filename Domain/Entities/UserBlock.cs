using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class UserBlock : EntityBase
    {
        [Required]
        public string BlockerProfileId { get; set; }
        public Profile BlockerProfile { get; set; }

        [Required]
        public string BlockedProfileId { get; set; }
        public Profile BlockedProfile { get; set; }
    }
} 