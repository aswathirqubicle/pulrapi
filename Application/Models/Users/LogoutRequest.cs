using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Models.Users
{
    public class LogoutRequest
    {
        [Required(ErrorMessage = "RefreshToken is required")]
        [StringLength(500, ErrorMessage = "RefreshToken cannot exceed 500 characters")]
        [RegularExpression(@"^[a-zA-Z0-9+/=_-]+$", ErrorMessage = "RefreshToken contains invalid characters")]
        public string RefreshToken { get; set; }
        
        [Required(ErrorMessage = "DeviceIdentifier is required")]
        [StringLength(100, ErrorMessage = "DeviceIdentifier cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "DeviceIdentifier contains invalid characters")]
        public string DeviceIdentifier { get; set; }
    }
} 