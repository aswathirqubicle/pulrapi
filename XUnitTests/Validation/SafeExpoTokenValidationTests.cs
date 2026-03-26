#if !PRODUCTION_BUILD
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;
using Xunit;

namespace XUnitTests.Validation;

public class SafeExpoTokenValidationTests
{
    [Fact]
    public void SafeExpoTokenAttribute_WithValidExponentPushToken_ShouldPassValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExponentPushToken[_PH3HtFFROyQAw58E7wzgS]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExponentPushToken[_PH3HtFFROyQAw58E7wzgS]", validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithValidExpoPushToken_ShouldPassValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExpoPushToken[_PH3HtFFROyQAw58E7wzgS]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExpoPushToken[_PH3HtFFROyQAw58E7wzgS]", validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithValidExpoToken_ShouldPassValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExpoToken[_PH3HtFFROyQAw58E7wzgS]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExpoToken[_PH3HtFFROyQAw58E7wzgS]", validationContext);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithInvalidTokenFormat_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "InvalidToken[_PH3HtFFROyQAw58E7wzgS]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("InvalidToken[_PH3HtFFROyQAw58E7wzgS]", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("valid Expo push token format", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithEmptyString_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("required", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithNullValue_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = (string)null }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("required", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithScriptTags_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExponentPushToken[<script>alert('xss')</script>]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExponentPushToken[<script>alert('xss')</script>]", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("script tags", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithDangerousCharacters_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExponentPushToken[<>&\"'`;(){}]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExponentPushToken[<>&\"'`;(){}]", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("invalid characters", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithTooShortToken_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 20);
        var validationContext = new ValidationContext(new { ExpoToken = "ExpoToken[123]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExpoToken[123]", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("at least 20 character(s) long", result!.ErrorMessage);
    }

    [Fact]
    public void SafeExpoTokenAttribute_WithTooLongToken_ShouldFailValidation()
    {
        // Arrange
        var attribute = new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 50, minLength: 10);
        var validationContext = new ValidationContext(new { ExpoToken = "ExponentPushToken[ThisIsAVeryLongTokenThatExceedsTheMaximumLengthLimit]" }) { MemberName = "ExpoToken" };

        // Act
        var result = attribute.GetValidationResult("ExponentPushToken[ThisIsAVeryLongTokenThatExceedsTheMaximumLengthLimit]", validationContext);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("cannot exceed 50 characters", result!.ErrorMessage);
    }
}
#endif
