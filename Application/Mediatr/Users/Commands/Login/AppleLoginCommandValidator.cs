using FluentValidation;
using Core.Application.Mediatr.Users.Commands.Login;
using System.Linq;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class AppleLoginCommandValidator : AbstractValidator<AppleLoginCommand>
    {
        public AppleLoginCommandValidator()
        {
            RuleFor(x => x.IdentityToken)
                .NotEmpty().WithMessage("identityToken is required.")
                .Must(IsValidJwtFormat).WithMessage("Invalid token format.");

            // Require device info and device identifier
            RuleFor(x => x.Device)
                .NotNull().WithMessage("device is required.");

            When(x => x.Device != null, () =>
            {
                RuleFor(x => x.Device.DeviceIdentifier)
                    .NotEmpty().WithMessage("device.deviceIdentifier is required.");

                // Optional: basic max lengths to avoid abuse
                RuleFor(x => x.Device.Brand)
                .NotEmpty().WithMessage("device.brand is required.")
                    .MaximumLength(100);
                RuleFor(x => x.Device.ModelName)
                .NotEmpty().WithMessage("device.modelName is required.")
                    .MaximumLength(150);
                RuleFor(x => x.Device.OsVersion)
                .NotEmpty().WithMessage("device.osVersion is required.")
                    .MaximumLength(50);
                RuleFor(x => x.Device.AppVersion)
                .NotEmpty().WithMessage("device.appVersion is required.")
                    .MaximumLength(50);
            });
        }

        private bool NotContainHtml(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            // Fast fail for angle brackets and the word 'script'
            if (value.Contains('<') || value.Contains('>')) return false;
            if (value.ToLower().Contains("script")) return false;
            return true;
        }

        private bool IsValidJwtFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // JWT should have exactly 3 parts separated by dots
            var parts = token.Split('.');
            return parts.Length == 3 && parts.All(p => !string.IsNullOrWhiteSpace(p));
        }
    }
}


