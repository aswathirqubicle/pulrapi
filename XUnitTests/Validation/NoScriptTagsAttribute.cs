// Core.Application/Validation/NoScriptTagsAttribute.cs
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Validation
{
    public class NoScriptTagsAttribute : ValidationAttribute
    {
        public NoScriptTagsAttribute()
        {
            ErrorMessage = "Field must not contain script tags";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string str && str.Contains("<script", StringComparison.OrdinalIgnoreCase))
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }
}