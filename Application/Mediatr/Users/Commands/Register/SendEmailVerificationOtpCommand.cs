using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
//using Core.Application.Exceptions;
using Core.Domain.Entities;
using Core.Application.Interfaces;
using System.Linq;

namespace Core.Application.Mediatr.Users.Commands.Register
{
    public class SendEmailVerificationOtpCommand : IRequest<EmailVerificationResponse>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class EmailVerificationResponse
    {
        public bool Success { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsVerified { get; set; }
        public string Message { get; set; }
    }

    public class SendEmailVerificationOtpCommandHandler : IRequestHandler<SendEmailVerificationOtpCommand, EmailVerificationResponse>
    {
        private readonly ILogger<SendEmailVerificationOtpCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public SendEmailVerificationOtpCommandHandler(
            ILogger<SendEmailVerificationOtpCommandHandler> logger,
            UserManager<User> userManager,
            IApplicationDbContext dbContext,
            IUserService userService)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task<EmailVerificationResponse> Handle(SendEmailVerificationOtpCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if email already exists and is not suspended
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && !existingUser.IsSuspended)
                {
                    if((existingUser.EmailConfirmed || existingUser.IsVerified))
                    {
                        return new EmailVerificationResponse
                        {
                            Success = false,
                            EmailConfirmed = existingUser.EmailConfirmed,
                            IsVerified = existingUser.IsVerified,
                            Message = "An account with this email address already exists."
                        };
                    }

                    await _userService.SendEmailConfirmationToken(existingUser);
                    return new EmailVerificationResponse
                    {
                        Success = true,
                        EmailConfirmed = existingUser.EmailConfirmed,
                        IsVerified = existingUser.IsVerified,
                        Message = "OTP resent successfully "
                    };
                }

                // If user exists but is suspended, use that user
                if (existingUser != null)
                {
                    await _userService.SendEmailConfirmationToken(existingUser);
                    return new EmailVerificationResponse
                    {
                        Success = true,
                        EmailConfirmed = existingUser.EmailConfirmed,
                        IsVerified = existingUser.IsVerified,
                        Message = "OTP sent successfully to suspended user."
                    };
                }
                else
                {
                    // Create temporary user for verification
                    var tempUser = new User
                    {
                        UserName = request.Email,
                        Email = request.Email,
                        IsSuspended = false
                    };

                    var result = await _userManager.CreateAsync(tempUser);
                    if (!result.Succeeded)
                    {
                        return new EmailVerificationResponse
                        {
                            Success = false,
                            EmailConfirmed = false,
                            IsVerified = false,
                            Message = string.Join(", ", result.Errors.Select(e => e.Description))
                        };
                    }

                    await _userService.SendEmailConfirmationToken(tempUser);
                    return new EmailVerificationResponse
                    {
                        Success = true,
                        EmailConfirmed = false,
                        IsVerified = false,
                        Message = "OTP sent successfully."
                    };
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new EmailVerificationResponse
                {
                    Success = false,
                    EmailConfirmed = false,
                    IsVerified = false,
                    Message = e.Message
                };
            }
        }
    }
} 