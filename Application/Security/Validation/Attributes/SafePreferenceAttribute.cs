using System.ComponentModel.DataAnnotations;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Validation attribute for individual preference strings that prevents SQL injection and XSS attacks
    /// </summary>
    public class SafePreferenceAttribute : OptimizedValidationBase
    {
        public SafePreferenceAttribute(bool allowNullValue = false, int maxLength = 100, int minLength = 1) 
            : base(allowNullValue, maxLength, minLength)
        {
        }
    }
}
