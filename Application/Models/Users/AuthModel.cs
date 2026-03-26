using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Models.Users
{
    public class AuthModel
    {
        public string Message { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
    }
}
