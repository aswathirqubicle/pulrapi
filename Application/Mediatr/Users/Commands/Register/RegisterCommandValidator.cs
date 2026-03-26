using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Mediatr.Users.Commands.Register;
using Core.Domain.Entities;
using Core.Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;

namespace Core.Application.Mediatr.Users.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;

        public RegisterCommandValidator(UserManager<User> userManager, IApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MustAsync(UniqueUsername).WithMessage("This username is already taken. Please choose a different one.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .MustAsync(UniqueEmail).WithMessage("This email is already registered. Please use a different email address.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MinimumLength(3).WithMessage("First name must be at least 3 characters long.")
                .Matches("^[a-zA-Z\\s]*$").WithMessage("First name can only contain letters and spaces.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .When(x => !x.IsSocialLogin);

            RuleFor(x => x.TermsAccepted)
                .Equal(true).WithMessage("You must accept the terms and conditions to register.");
        }

        private async Task<bool> UniqueUsername(string username, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(username)) return true;
            var normalizedUsername = UsernameHelper.Normalize(username);
            return await _userManager.FindByNameAsync(normalizedUsername) == null;
        }

        private async Task<bool> UniqueEmail(string email, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(email)) return true;
            var user = await _userManager.FindByEmailAsync(email);
            
            // Allow registration if:
            // 1. No user exists with this email
            // 2. User exists but is suspended
            // 3. User exists and is verified (temporary user for email verification)
            return user == null || user.IsSuspended || (user.EmailConfirmed);
        }
    }
}
