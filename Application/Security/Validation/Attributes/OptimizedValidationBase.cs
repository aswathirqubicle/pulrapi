using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Core.Application.Security.Validation.Attributes
{
    /// <summary>
    /// Base class for optimized validation attributes with shared regex patterns and performance optimizations
    /// </summary>
    public abstract class OptimizedValidationBase : ValidationAttribute
    {
        // Static compiled regex patterns for better performance - compiled once and reused
        private static readonly Regex DangerousCharsPattern = new Regex("[<>\"'`;(){}[\\]\\\\|&$*?!#@%^+=~]", RegexOptions.Compiled);
        private static readonly Regex ScriptPattern = new Regex("<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlPattern = new Regex("<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        // Cache for field names to avoid repeated string operations
        private static readonly Dictionary<string, string> FieldNameCache = new Dictionary<string, string>();

        protected readonly bool AllowNullValue;
        protected readonly int MaxLength;
        protected readonly int MinLength;

        protected OptimizedValidationBase(bool allowNullValue = false, int maxLength = 100, int minLength = 1)
        {
            AllowNullValue = allowNullValue;
            MaxLength = maxLength;
            MinLength = minLength;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Early return for null values
            if (AllowNullValue && value == null)
            {
                return ValidationResult.Success;
            }

            string input = value?.ToString();

            // Early return for null/empty strings
            if (string.IsNullOrWhiteSpace(input))
            {
                return AllowNullValue ? ValidationResult.Success : CreateErrorResult(GetCachedFieldName(validationContext), "is required.");
            }

            // Early return for length validation (fastest checks first)
            if (input.Length < MinLength)
            {
                return CreateErrorResult(GetCachedFieldName(validationContext), $"must be at least {MinLength} character(s) long.");
            }

            if (input.Length > MaxLength)
            {
                return CreateErrorResult(GetCachedFieldName(validationContext), $"cannot exceed {MaxLength} characters.");
            }

            // Security validation (most expensive checks last)
            return ValidateSecurity(input, validationContext);
        }

        protected virtual ValidationResult ValidateSecurity(string input, ValidationContext validationContext)
        {
            string fieldName = GetCachedFieldName(validationContext);

            // Check for script tags first (most dangerous)
            if (ScriptPattern.IsMatch(input))
            {
                return CreateErrorResult(fieldName, "cannot contain script tags.");
            }

            // Check for HTML tags
            if (HtmlPattern.IsMatch(input))
            {
                return CreateErrorResult(fieldName, "cannot contain HTML tags.");
            }

            // Check for dangerous characters last (least specific)
            if (DangerousCharsPattern.IsMatch(input))
            {
                return CreateErrorResult(fieldName, "contains invalid characters.");
            }

            return ValidationResult.Success;
        }

        protected ValidationResult CreateErrorResult(string fieldName, string message)
        {
            return new ValidationResult($"{fieldName} {message}");
        }

        protected string GetCachedFieldName(ValidationContext validationContext)
        {
            string key = $"{validationContext.ObjectType.Name}.{validationContext.MemberName}";
            
            if (!FieldNameCache.TryGetValue(key, out string fieldName))
            {
                fieldName = validationContext.DisplayName ?? validationContext.MemberName ?? "Field";
                FieldNameCache[key] = fieldName;
            }

            return fieldName;
        }
    }
}
