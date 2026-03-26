using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Security.Validation.Attributes
{
    public class ValidFormImagePrioritiesAttribute : ValidationAttribute
    {

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var values = (List<string>)value;

            foreach (var val in values)
            {
                int valueParsed;

                if (!int.TryParse((string)val, out valueParsed))
                {
                    return new ValidationResult("Priority must be a number.");
                }

                if (values.Count > 5)
                {
                    return new ValidationResult("Max 5 images allowed.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
