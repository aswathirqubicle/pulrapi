using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class PasswordChangeFromEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public string Token { get; set; }
    }
}
