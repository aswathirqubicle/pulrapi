using FluentValidation;
using Core.Application.Mediatr.Users.Commands.Login;
using System.Text.RegularExpressions;
using System.Linq;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        // Regex patterns for detecting malicious inputs
        private static readonly Regex SqlInjectionPattern = new Regex(
            @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|UNION|SCRIPT)\b|'|(OR|AND)\s+\d+\s*=\s*\d+|--|/\*|\*/|xp_|sp_)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex XssPattern = new Regex(
            @"<script|javascript:|on\w+\s*=|<\s*iframe|<\s*object|<\s*embed|<\s*link|<\s*meta|vbscript:|data:|<.*>.*</.*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Updated regex to support Apple Private Relay addresses and other valid email formats
        private static readonly Regex EmailPattern = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.([a-zA-Z]{2,}|privaterelay\.appleid\.com)$",
            RegexOptions.Compiled);

        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Must(NotContainMaliciousContent).WithMessage("Invalid input format detected.");

            RuleFor(x => x.Device)
                .NotNull().WithMessage("Device information is required.");

            When(x => x.IsEmail, () =>
            {
                RuleFor(x => x.Username)
                    .Must(BeValidEmailFormat).WithMessage("Invalid email format.");
            });
        }

        private static bool BeValidInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Check for basic input validity - no null bytes, control characters, etc.
            return !input.Any(c => char.IsControl(c) && c != '\t' && c != '\n' && c != '\r');
        }

        private static bool NotContainMaliciousContent(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            // Check for SQL injection patterns
            if (SqlInjectionPattern.IsMatch(input))
                return false;

            // Check for XSS patterns
            if (XssPattern.IsMatch(input))
                return false;

            return true;
        }

        private static bool BeValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailPattern.IsMatch(email);
        }
    }
}


