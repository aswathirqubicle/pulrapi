using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Utilities
{
    public static class ControllerValidationExtensions
    {
        // Common validation helper for controller parameters
        // Returns ActionResult when invalid, otherwise null to proceed
        public static ActionResult ValidateWithAttribute(this ControllerBase controller, string value, ValidationAttribute attribute, string memberName, int statusCode)
        {
            var validationContext = new ValidationContext(new { value }) { MemberName = memberName };
            var validationResult = attribute.GetValidationResult(value, validationContext);
            if (validationResult != ValidationResult.Success)
            {
                return controller.StatusCode(statusCode, new { message = validationResult.ErrorMessage });
            }
            return null;
        }
    }
}


