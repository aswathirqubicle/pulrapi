namespace Core.Application.Models.Users
{
    public class AdminDeleteUserRequest
    {
        public string Username { get; set; }
        public string SecretCode { get; set; }
    }
} 