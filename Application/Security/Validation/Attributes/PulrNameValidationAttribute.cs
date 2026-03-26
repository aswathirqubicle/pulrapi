using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    public class PulrNameValidationAttribute : ValidationAttribute
    {
        private readonly bool _allowNullValue;
        private readonly Regex _regexPattern = new Regex("^(?=.*[a-zA-Z].*[a-zA-Z]).*$");

        public PulrNameValidationAttribute(bool allowNullValue = false)
        {
            _allowNullValue = allowNullValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if(_allowNullValue && value == null)
            {
                return ValidationResult.Success;
            }

            if (!_regexPattern.IsMatch(value.ToString()))
            {
                return new ValidationResult("Value must have at least 2 letters.");
            }

            return ValidationResult.Success;
        }
    }
}
