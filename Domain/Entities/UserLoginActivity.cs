using System;

namespace Core.Domain.Entities
{
    public class UserLoginActivity : EntityBase
    {
        public string UserId { get; set; }
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string OsVersion { get; set; }
        public string DeviceIdentifier { get; set; }
        public string Action { get; set; } // "Logged in" or "Logged out"
        public DateTime Timestamp { get; set; }
        public string AppVersion { get; set; }
        public virtual User User { get; set; }
    }
} 