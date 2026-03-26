using Core.Domain.Common;
using System;

namespace Core.Domain.Entities
{
    public class RefreshToken : EntityBase
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string DeviceIdentifier { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string ReplacedByToken { get; set; }
        public new bool IsActive => RevokedAt == null && !IsExpired;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public virtual User User { get; set; }
    }
} 