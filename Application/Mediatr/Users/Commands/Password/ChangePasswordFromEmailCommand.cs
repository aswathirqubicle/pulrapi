using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Domain.Entities;
using Core.Application.Interfaces;

using ValidationException = Core.Application.Exceptions.ValidationException;

namespace Core.Application.Mediatr.Users.Commands.Password
{
    public class ChangePasswordFromEmailCommand : IRequest <Unit>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and password confirmation do not match.")]
        public string PasswordConfirmation { get; set; }

        [Required]
        public string Otp { get; set; }
    }

    public class ChangePasswordFromEmailCommandHandler : IRequestHandler<ChangePasswordFromEmailCommand,Unit>
    {
        private readonly ILogger<ChangePasswordFromEmailCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;

        public ChangePasswordFromEmailCommandHandler(
            ILogger<ChangePasswordFromEmailCommandHandler> logger,
            UserManager<User> userManager,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(ChangePasswordFromEmailCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                // Verify OTP first
                if (user.PasswordResetCode != request.Otp)
                {
                    throw new ValidationException("Invalid OTP.");
                }

                if (!user.PasswordResetCodeExpiry.HasValue || user.PasswordResetCodeExpiry < DateTime.UtcNow)
                {
                    // throw new ValidationException("OTP has expired.");
                    throw new ValidationException("We couldn’t verify your request. Please try again.");
                }

                // Remove old password and set new one
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    throw new ValidationException("Failed to remove old password.");
                }

                var addResult = await _userManager.AddPasswordAsync(user, request.Password);
                if (!addResult.Succeeded)
                {
                    throw new ValidationException("Failed to set new password.");
                }

                // Clear the OTP so it can't be reused
                user.PasswordResetCode = null;
                user.PasswordResetCodeExpiry = null;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
