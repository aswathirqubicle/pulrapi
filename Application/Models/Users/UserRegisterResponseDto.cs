using Core.Domain.Entities;

namespace Core.Application.Models.Users
{
    public class UserRegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public User User { get; set; }
        public string Message { get; set; }
        public bool IsNewUser { get; set; } // true if user was newly created
        public bool HasCompletedOnboarding { get; set; }
    }
}
