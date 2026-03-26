#if !PRODUCTION_BUILD
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace XUnitTests.Validation;

public class OnboardingPreferencesValidationTests
{
    [Fact]
    public void SafePreferenceAttribute_WithSqlInjectionAttempt_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
        var validationContext = new ValidationContext(new { Preference = "' OR 1=1--" }) { MemberName = "Preference" };

        // Act
        var result = attribute.GetValidationResult("' OR 1=1--", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("invalid characters", result!.ErrorMessage);
    }

    [Fact]
    public void SafePreferenceAttribute_WithScriptTags_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
        var validationContext = new ValidationContext(new { Preference = "<script>alert('xss')</script>" }) { MemberName = "Preference" };

        // Act
        var result = attribute.GetValidationResult("<script>alert('xss')</script>", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("script tags", result!.ErrorMessage);
    }

    [Fact]
    public void SafePreferenceAttribute_WithValidPreference_ShouldPassValidation()
    {
        // Arrange
        var attribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
        var validationContext = new ValidationContext(new { Preference = "valid-preference" }) { MemberName = "Preference" };

        // Act
        var result = attribute.GetValidationResult("valid-preference", validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void SafePreferenceAttribute_WithEmptyString_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
        var validationContext = new ValidationContext(new { Preference = "" }) { MemberName = "Preference" };

        // Act
        var result = attribute.GetValidationResult("", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("required", result!.ErrorMessage);
    }

    [Fact]
    public void SafePreferenceAttribute_WithNullValue_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
        var validationContext = new ValidationContext(new { Preference = (string)null }) { MemberName = "Preference" };

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("required", result!.ErrorMessage);
    }
}
#endif