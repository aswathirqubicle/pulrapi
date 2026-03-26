using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class CheckEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
} 