using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class SignOutAllDevicesRequest
    {
        [Required(ErrorMessage = "CurrentDeviceIdentifier is required")]
        [StringLength(100, ErrorMessage = "CurrentDeviceIdentifier cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "CurrentDeviceIdentifier contains invalid characters.")]
        public string CurrentDeviceIdentifier { get; set; }
    }
} 