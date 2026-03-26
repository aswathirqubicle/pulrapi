using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Optimized validation attribute for UID fields with GUID format validation and improved performance
    /// </summary>
    public class SafeUidAttribute : OptimizedValidationBase
    {
        // Compiled regex for GUID format validation (better performance than Guid.TryParse for invalid formats)
        private static readonly Regex GuidFormatPattern = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        
        private readonly bool _validateGuidFormat;

        public SafeUidAttribute(bool allowNullValue = false, int maxLength = 50, int minLength = 1, bool validateGuidFormat = true) 
            : base(allowNullValue, maxLength, minLength)
        {
            _validateGuidFormat = validateGuidFormat;
        }

        protected override ValidationResult ValidateSecurity(string input, ValidationContext validationContext)
        {
            string fieldName = GetCachedFieldName(validationContext);

            // First validate GUID format if required
            if (_validateGuidFormat)
            {
                if (!GuidFormatPattern.IsMatch(input))
                {
                    return CreateErrorResult(fieldName, "must be a valid GUID format.");
                }

                // Double-check with Guid.TryParse for complete validation
                if (!Guid.TryParse(input, out _))
                {
                    return CreateErrorResult(fieldName, "must be a valid GUID format.");
                }
            }

            // Then run the base security validation (XSS, HTML tags, dangerous characters)
            return base.ValidateSecurity(input, validationContext);
        }
    }
}
