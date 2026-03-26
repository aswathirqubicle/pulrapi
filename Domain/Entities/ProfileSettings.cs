using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProfileSettings : EntityBase
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public bool IsProfilePublic { get; set; } = true;
        public bool ShowSocialMediaLinks { get; set; } = true;
        public bool ShowFollowers { get; set; } = true;
        public bool ShowFollowing { get; set; } = true;
        public bool ShowLocation { get; set; } = true;
        public bool ShowAbout { get; set; } = true;
    }
} 