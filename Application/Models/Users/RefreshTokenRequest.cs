using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
        [Required]
        public string DeviceIdentifier { get; set; }
    }
} 