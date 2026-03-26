using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Validation attribute for comment content that allows social media characters (@, #, !, $, etc.)
    /// but prevents dangerous characters and scripts
    /// </summary>
    public class SafeCommentAttribute : ValidationAttribute
    {
        private static readonly Regex DangerousCharsPattern = new Regex("[<>\"'`]", RegexOptions.Compiled);
        private static readonly Regex ScriptPattern = new Regex("<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlPattern = new Regex("<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool AllowNullValue { get; set; }
        public int MaxLength { get; set; }
        public int MinLength { get; set; }

        public SafeCommentAttribute(bool allowNullValue = false, int maxLength = 1000, int minLength = 1)
        {
            AllowNullValue = allowNullValue;
            MaxLength = maxLength;
            MinLength = minLength;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Handle null values
            if (AllowNullValue && value == null)
                return ValidationResult.Success;

            string input = value?.ToString();

            // Handle empty strings
            if (string.IsNullOrWhiteSpace(input))
                return AllowNullValue ? ValidationResult.Success : new ValidationResult($"{validationContext.DisplayName} is required.");

            // Length validation
            if (input.Length < MinLength)
                return new ValidationResult($"{validationContext.DisplayName} must be at least {MinLength} character(s) long.");

            if (input.Length > MaxLength)
                return new ValidationResult($"{validationContext.DisplayName} cannot exceed {MaxLength} characters.");

            // Security validation
            string fieldName = validationContext.DisplayName ?? validationContext.MemberName ?? "Field";

            if (ScriptPattern.IsMatch(input))
                return new ValidationResult($"{fieldName} cannot contain script tags.");

            if (HtmlPattern.IsMatch(input))
                return new ValidationResult($"{fieldName} cannot contain HTML tags.");

            if (DangerousCharsPattern.IsMatch(input))
                return new ValidationResult($"{fieldName} contains invalid characters.");

            return ValidationResult.Success;
        }
    }
}