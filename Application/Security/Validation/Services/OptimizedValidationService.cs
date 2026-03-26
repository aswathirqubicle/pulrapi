using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Application.Security.Validation.Services
{
    /// <summary>
    /// High-performance validation service with caching and bulk operations
    /// </summary>
    public class OptimizedValidationService
    {
        // Thread-safe cache for validation results
        private static readonly ConcurrentDictionary<string, ValidationResult> ValidationCache = new();
        
        // Compiled regex patterns for maximum performance
        private static readonly Regex DangerousCharsPattern = new Regex("[<>\"'`;(){}[\\]\\\\|&$*?!#@%^+=~]", RegexOptions.Compiled);
        private static readonly Regex ScriptPattern = new Regex("<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlPattern = new Regex("<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Validates a single input with caching for repeated values
        /// </summary>
        public ValidationResult ValidateInput(string input, string fieldName, bool allowNull = false, int maxLength = 100, int minLength = 1)
        {
            if (string.IsNullOrEmpty(input))
            {
                return allowNull ? ValidationResult.Success : new ValidationResult($"{fieldName} is required.");
            }

            // Create cache key
            string cacheKey = $"{input}|{fieldName}|{allowNull}|{maxLength}|{minLength}";
            
            // Check cache first
            if (ValidationCache.TryGetValue(cacheKey, out ValidationResult cachedResult))
            {
                return cachedResult;
            }

            // Perform validation
            ValidationResult result = PerformValidation(input, fieldName, allowNull, maxLength, minLength);
            
            // Cache the result (only cache successful validations to avoid memory bloat)
            if (result == ValidationResult.Success)
            {
                ValidationCache.TryAdd(cacheKey, result);
            }

            return result;
        }

        /// <summary>
        /// Validates multiple inputs in batch for better performance
        /// </summary>
        public Dictionary<string, ValidationResult> ValidateBatch(Dictionary<string, (string value, bool allowNull, int maxLength, int minLength)> inputs)
        {
            var results = new Dictionary<string, ValidationResult>(inputs.Count);

            // Use parallel processing only for larger batches to avoid overhead
            if (inputs.Count > 50)
            {
                var lockObject = new object();
                Parallel.ForEach(inputs, kvp =>
                {
                    var result = ValidateInput(kvp.Value.value, kvp.Key, kvp.Value.allowNull, kvp.Value.maxLength, kvp.Value.minLength);
                    lock (lockObject)
                    {
                        results[kvp.Key] = result;
                    }
                });
            }
            else
            {
                // Sequential processing for small batches avoids parallelization overhead
                foreach (var kvp in inputs)
                {
                    results[kvp.Key] = ValidateInput(kvp.Value.value, kvp.Key, kvp.Value.allowNull, kvp.Value.maxLength, kvp.Value.minLength);
                }
            }

            return results;
        }

        /// <summary>
        /// Fast validation for common patterns without caching overhead
        /// </summary>
        public bool IsValidInput(string input, int maxLength = 100, int minLength = 1)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.Length < minLength || input.Length > maxLength)
                return false;

            // Quick character checks for common dangerous patterns
            if (input.Contains('<') || input.Contains('>') || input.Contains('"') || input.Contains("'"))
                return false;

            return true;
        }

        /// <summary>
        /// Clears the validation cache (useful for testing or memory management)
        /// </summary>
        public static void ClearCache()
        {
            ValidationCache.Clear();
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public static (int Count, long MemoryEstimate) GetCacheStats()
        {
            return (ValidationCache.Count, ValidationCache.Count * 100); // Rough estimate
        }

        private ValidationResult PerformValidation(string input, string fieldName, bool allowNull, int maxLength, int minLength)
        {
            // Length validation (fastest)
            if (input.Length < minLength)
            {
                return new ValidationResult($"{fieldName} must be at least {minLength} character(s) long.");
            }

            if (input.Length > maxLength)
            {
                return new ValidationResult($"{fieldName} cannot exceed {maxLength} characters.");
            }

            // Security validation (most expensive)
            if (ScriptPattern.IsMatch(input))
            {
                return new ValidationResult($"{fieldName} cannot contain script tags.");
            }

            if (HtmlPattern.IsMatch(input))
            {
                return new ValidationResult($"{fieldName} cannot contain HTML tags.");
            }

            if (DangerousCharsPattern.IsMatch(input))
            {
                return new ValidationResult($"{fieldName} contains invalid characters.");
            }

            return ValidationResult.Success;
        }
    }
}
