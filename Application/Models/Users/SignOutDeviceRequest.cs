using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class SignOutDeviceRequest
    {
        [Required(ErrorMessage = "DeviceIdentifier is required")]
        [StringLength(100, ErrorMessage = "DeviceIdentifier cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "DeviceIdentifier contains invalid characters. Only alphanumeric characters, hyphens, and underscores are allowed.")]
        public string DeviceIdentifier { get; set; }
    }
} 