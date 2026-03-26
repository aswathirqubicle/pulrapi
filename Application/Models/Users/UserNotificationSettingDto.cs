using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class UserNotificationSettingDto
    {
        [Required]
        public string DeviceId { get; set; }
        [Required]
        public string PushToken { get; set; }
        public bool? Likes { get; set; }
        public bool? Comments { get; set; }
        public bool? Mentions { get; set; }
        public bool? Follows { get; set; }
        public bool? SavedPosts { get; set; }
        public bool? ShopActivity { get; set; }
        public bool? DirectMessages { get; set; }
        public bool? EmailNotification { get; set; }
    }

    public class UpdateUserNotificationSettingDto
    {
        public string DeviceId { get; set; }
        public string PushToken { get; set; }
        public bool? Likes { get; set; }
        public bool? Comments { get; set; }
        public bool? Mentions { get; set; }
        public bool? Follows { get; set; }
        public bool? SavedPosts { get; set; }
        public bool? ShopActivity { get; set; }
        public bool? DirectMessages { get; set; }
        public bool? EmailNotification { get; set; }
    }
} 