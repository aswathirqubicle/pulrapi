using System.ComponentModel.DataAnnotations;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Optimized validation attribute for name fields with improved performance and caching
    /// </summary>
    public class SafeNameAttribute : OptimizedValidationBase
    {
        public SafeNameAttribute(bool allowNullValue = false, int maxLength = 100, int minLength = 1) 
            : base(allowNullValue, maxLength, minLength)
        {
        }
    }
}
