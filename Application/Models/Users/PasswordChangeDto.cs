using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class PasswordChangeDto
    {
        [Required]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
