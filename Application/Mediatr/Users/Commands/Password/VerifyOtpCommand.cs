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
    public class VerifyOtpCommand : IRequest<bool>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }

        public bool IsEmailVerification { get; set; } = false;
    }

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, bool>
    {
        private readonly ILogger<VerifyOtpCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;

        public VerifyOtpCommandHandler(
            ILogger<VerifyOtpCommandHandler> logger,
            UserManager<User> userManager,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                if (request.IsEmailVerification)
                {
                    // Check if email verification OTP matches and hasn't expired
                    if (user.EmailVerificationCode != request.Otp)
                    {
                        throw new ValidationException("Invalid OTP.");
                    }

                    if (!user.EmailVerificationCodeExpiry.HasValue || user.EmailVerificationCodeExpiry < DateTime.UtcNow)
                    {
                        throw new ValidationException("OTP has expired.");
                    }

                    // Mark email as confirmed and user as verified
                    user.EmailConfirmed = true;
                    user.EmailVerificationCode = null;
                    user.EmailVerificationCodeExpiry = null;
                }
                else
                {
                    // Check if password reset OTP matches and hasn't expired
                    if (user.PasswordResetCode != request.Otp)
                    {
                        throw new ValidationException("Invalid OTP.");
                    }

                    if (!user.PasswordResetCodeExpiry.HasValue || user.PasswordResetCodeExpiry < DateTime.UtcNow)
                    {
                        // throw new ValidationException("The password reset code has expired. Please request a new code to reset your password.");
                        throw new ValidationException("We couldn’t verify your request. Please try again.");
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
} 