using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs
{
    public class ProfileSettingsDto
    {
        public bool IsProfilePublic { get; set; }
        public bool ShowSocialMediaLinks { get; set; }
        public bool ShowFollowers { get; set; }
        public bool ShowFollowing { get; set; }
        public bool ShowLocation { get; set; }
        public bool ShowAbout { get; set; }
    }

    public class UpdateProfileSettingsDto
    {
        public bool? IsProfilePublic { get; set; }
        public bool? ShowSocialMediaLinks { get; set; }
        public bool? ShowFollowers { get; set; }
        public bool? ShowFollowing { get; set; }
        public bool? ShowLocation { get; set; }
        public bool? ShowAbout { get; set; }
    }
} 