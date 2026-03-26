using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Optimized validation attribute for Expo push token fields with consistent validation rules
    /// </summary>
    public class SafeExpoTokenAttribute : OptimizedValidationBase
    {
        // Compiled regex for Expo token format validation - supports ExponentPushToken, ExpoPushToken, and ExpoToken prefixes
        private static readonly Regex ExpoTokenPattern = new Regex(@"^(ExponentPushToken\[|ExpoPushToken\[|ExpoToken\[)", RegexOptions.Compiled);
        
        public SafeExpoTokenAttribute(bool allowNullValue = false, int maxLength = 512, int minLength = 10) 
            : base(allowNullValue, maxLength, minLength)
        {
        }

        protected override ValidationResult ValidateSecurity(string input, ValidationContext validationContext)
        {
            string fieldName = GetCachedFieldName(validationContext);

            // Validate Expo token format - must start with valid Expo token prefix
            if (!ExpoTokenPattern.IsMatch(input))
            {
                return CreateErrorResult(fieldName, "must be a valid Expo push token format (ExponentPushToken[...], ExpoPushToken[...], or ExpoToken[...]).");
            }

            // For Expo tokens, we need to allow square brackets and other characters that are part of the token format
            // So we'll do a more targeted security validation instead of the base validation
            return ValidateExpoTokenSecurity(input, validationContext);
        }

        private ValidationResult ValidateExpoTokenSecurity(string input, ValidationContext validationContext)
        {
            string fieldName = GetCachedFieldName(validationContext);

            // Check for script tags first (most dangerous)
            if (System.Text.RegularExpressions.Regex.IsMatch(input, "<script[^>]*>.*?</script>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return CreateErrorResult(fieldName, "cannot contain script tags.");
            }

            // Check for HTML tags
            if (System.Text.RegularExpressions.Regex.IsMatch(input, "<[^>]+>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return CreateErrorResult(fieldName, "cannot contain HTML tags.");
            }

            // Check for dangerous characters but allow square brackets and other Expo token characters
            if (System.Text.RegularExpressions.Regex.IsMatch(input, "[<>\"'`;(){}|&$*?!#@%^+=~]"))
            {
                return CreateErrorResult(fieldName, "contains invalid characters.");
            }

            return ValidationResult.Success;
        }
    }
}
