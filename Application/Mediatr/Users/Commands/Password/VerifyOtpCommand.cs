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
        // After this many failed verifications the code is invalidated and a new one must be requested.
        private const int MaxOtpAttempts = 5;

        private readonly ILogger<VerifyOtpCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly IOtpHasher _otpHasher;

        public VerifyOtpCommandHandler(
            ILogger<VerifyOtpCommandHandler> logger,
            UserManager<User> userManager,
            IApplicationDbContext dbContext,
            IOtpHasher otpHasher)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _otpHasher = otpHasher;
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
                    // Compare against the stored hash, tracking failed attempts.
                    if (!_otpHasher.Verify(request.Otp, user.EmailVerificationCode))
                    {
                        user.EmailVerificationAttempts++;
                        if (user.EmailVerificationAttempts >= MaxOtpAttempts)
                        {
                            // Too many failures: invalidate the code so it can't be brute-forced further.
                            user.EmailVerificationCode = null;
                            user.EmailVerificationCodeExpiry = null;
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
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
                    user.EmailVerificationAttempts = 0;
                }
                else
                {
                    // Compare against the stored hash, tracking failed attempts.
                    if (!_otpHasher.Verify(request.Otp, user.PasswordResetCode))
                    {
                        user.PasswordResetAttempts++;
                        if (user.PasswordResetAttempts >= MaxOtpAttempts)
                        {
                            user.PasswordResetCode = null;
                            user.PasswordResetCodeExpiry = null;
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        throw new ValidationException("Invalid OTP.");
                    }

                    if (!user.PasswordResetCodeExpiry.HasValue || user.PasswordResetCodeExpiry < DateTime.UtcNow)
                    {
                        // throw new ValidationException("The password reset code has expired. Please request a new code to reset your password.");
                        throw new ValidationException("We couldn’t verify your request. Please try again.");
                    }

                    // Successful verification resets the failed-attempt counter.
                    user.PasswordResetAttempts = 0;
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