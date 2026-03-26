using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Profile : EntityBase
    {
        public string About { get; set; }
        public string ImageUrl { get; set; }

        [Required]
        public string UserId { get; set; }

        public User User { get; set; }
        public int GenderId { get; set; }
        public Gender Gender { get; set; }
        public string Location { get; set; }
        public int? CurrencyId { get; set; }
        public Currency Currency { get; set; }
        public string UserType { get; set; }

        public ProfileSocialMedia ProfileSocialMedia { get; set; }
        public ProfileSettings ProfileSettings { get; set; }
        public virtual ICollection<ProfileSocialMediaLink> ProfileSocialMediaLinks { get; set; } = new List<ProfileSocialMediaLink>();
        public virtual ICollection<ProfileFollower> ProfileFollowers { get; set; }
        public virtual ICollection<ProfileFollower> ProfileFollowings { get; set; }
        public virtual ICollection<PostMyStyle> PostMyStyles { get; set; }
        public virtual ICollection<StoryLike> StoryLikes { get; set; } = new List<StoryLike>();
        public virtual ICollection<ProfileOnboardingPreference> ProfileOnboardingPreferences { get; set; }
        public virtual ICollection<ProfileVibe> ProfileVibes { get; set; } = new List<ProfileVibe>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
        public virtual ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
        public virtual ICollection<UserBlock> BlockedByUsers { get; set; } = new List<UserBlock>();
    }
}