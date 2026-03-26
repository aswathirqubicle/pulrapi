using System;

namespace Core.Application.Models.Users
{
    public class LoginActivityDto
    {
        public string DeviceName { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 