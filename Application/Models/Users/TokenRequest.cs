using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class TokenRequest
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public bool IsEmail { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
