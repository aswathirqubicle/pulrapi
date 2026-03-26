using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Optimized validation attribute for device ID fields with consistent validation rules
    /// </summary>
    public class SafeDeviceIdAttribute : OptimizedValidationBase
    {
        // Compiled regex for device ID format validation - allows letters, numbers, underscore, colon, dot, hyphen
        private static readonly Regex DeviceIdPattern = new Regex(@"^[A-Za-z0-9_:.-]+$", RegexOptions.Compiled);
        
        public SafeDeviceIdAttribute(bool allowNullValue = false, int maxLength = 128, int minLength = 3) 
            : base(allowNullValue, maxLength, minLength)
        {
        }

        protected override ValidationResult ValidateSecurity(string input, ValidationContext validationContext)
        {
            string fieldName = GetCachedFieldName(validationContext);

            // Validate device ID format - must contain only allowed characters
            if (!DeviceIdPattern.IsMatch(input))
            {
                return CreateErrorResult(fieldName, "must contain only letters, numbers, underscore, colon, dot, and hyphen.");
            }

            // Run the base security validation (XSS, HTML tags, dangerous characters)
            return base.ValidateSecurity(input, validationContext);
        }
    }
}
