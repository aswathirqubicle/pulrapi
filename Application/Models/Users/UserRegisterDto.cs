using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class UserRegisterDto
    {
        [SafeName(allowNullValue: false, maxLength: 50, minLength: 1, ErrorMessage = "First name contains invalid characters or format.")]
        public string FirstName { get; set; }
        
        [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "Last name contains invalid characters or format.")]
        public string LastName { get; set; }
        
        [SafeName(maxLength: 50, minLength: 1, allowNullValue: true, ErrorMessage = "Display name contains invalid characters or format.")]
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        
        [SafeName(allowNullValue: false, maxLength: 30, minLength: 3, ErrorMessage = "Username contains invalid characters or format.")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
        public string Password { get; set; }
        public string CommunicationMail { get; set; }
        public bool IsSocialLogin { get; set; } // true for Google/Apple registration
        
        [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
        public string CountryUid { get; internal set; }
        public GenderEnum? Gender { get; internal set; }
        
        [Required(ErrorMessage = "Terms acceptance is required.")]
        public bool TermsAccepted { get; set; }
        
        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime DateOfBirth { get; set; }
        public string UserType { get; set; }
    }
}
