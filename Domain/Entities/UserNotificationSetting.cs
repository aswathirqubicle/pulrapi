using System;

namespace Core.Domain.Entities
{
    public class UserNotificationSetting : EntityBase
    {
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public string PushToken { get; set; }
        public bool Likes { get; set; }
        public bool Comments { get; set; }
        public bool Mentions { get; set; }
        public bool Follows { get; set; }
        public bool SavedPosts { get; set; }
        public bool ShopActivity { get; set; }
        public bool DirectMessages { get; set; }
        public bool EmailNotification { get; set; }
        public virtual User User { get; set; }
    }
} 