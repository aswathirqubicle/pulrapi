using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    public class PulrUsernameValidationAttribute : ValidationAttribute
    {
        private readonly bool _allowNullValue;
        private readonly Regex _minLengthPattern = new Regex("^.{3,}$");
        private readonly Regex _maxLengthPattern = new Regex("^.{3,30}$");
        private readonly Regex _consecutiveCharsPattern = new Regex("(.)\\1{2,}");
        private readonly Regex _allowedCharsPattern = new Regex("^[a-zA-Z0-9._-]+$");
        private readonly Regex _dangerousCharsPattern = new Regex("[<>\"'`;(){}[\\]\\\\|&$*?!#@%^+=~]");

        public PulrUsernameValidationAttribute(bool allowNullValue = false)
        {
            _allowNullValue = allowNullValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (_allowNullValue && value == null)
            {
                return ValidationResult.Success;
            }

            string username = value?.ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                return new ValidationResult("Username cannot be empty.");
            }

            if (!_minLengthPattern.IsMatch(username))
            {
                return new ValidationResult("Username must be at least 3 characters long.");
            }

            if (!_maxLengthPattern.IsMatch(username))
            {
                return new ValidationResult("Username cannot exceed 30 characters.");
            }

            if (_consecutiveCharsPattern.IsMatch(username))
            {
                return new ValidationResult("Username cannot contain 3 or more consecutive identical characters.");
            }

            if (!_allowedCharsPattern.IsMatch(username))
            {
                return new ValidationResult("Username can only contain letters, numbers, dots, underscores, and hyphens.");
            }

            if (_dangerousCharsPattern.IsMatch(username))
            {
                return new ValidationResult("Username contains invalid characters.");
            }

            return ValidationResult.Success;
        }
    }
} 