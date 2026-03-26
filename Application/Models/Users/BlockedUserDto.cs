using System;

namespace Core.Application.Models.Users
{
    public class BlockedUserDto
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ImageUrl { get; set; }
        public DateTime BlockedAt { get; set; }
    }
} 