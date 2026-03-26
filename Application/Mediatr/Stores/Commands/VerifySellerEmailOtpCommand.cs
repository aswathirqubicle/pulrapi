using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ValidationException = Core.Application.Exceptions.ValidationException;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class VerifySellerEmailOtpCommand : IRequest<VerifySellerEmailOtpResponse>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }

    public class VerifySellerEmailOtpResponse
    {
        public bool Success { get; set; }
        public bool EmailVerified { get; set; }
        public string Message { get; set; }
    }

    public class VerifySellerEmailOtpCommandHandler : IRequestHandler<VerifySellerEmailOtpCommand, VerifySellerEmailOtpResponse>
    {
        private readonly ILogger<VerifySellerEmailOtpCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public VerifySellerEmailOtpCommandHandler(
            ILogger<VerifySellerEmailOtpCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<VerifySellerEmailOtpResponse> Handle(VerifySellerEmailOtpCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();

                var sellerSettings = await _dbContext.SellerSettings
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                if (sellerSettings == null)
                {
                    throw new NotFoundException("Seller settings not found.");
                }

                // Check if pending email exists
                if (string.IsNullOrEmpty(sellerSettings.PendingEmail))
                {
                    throw new ValidationException("No email verification in progress.");
                }

                // Check if email matches the pending email
                if (sellerSettings.PendingEmail != request.Email)
                {
                    throw new ValidationException("Email address does not match the one that received the OTP.");
                }

                // Check if OTP matches
                if (sellerSettings.EmailVerificationCode != request.Otp)
                {
                    throw new ValidationException("Invalid OTP.");
                }

                // Check if OTP has expired
                if (!sellerSettings.EmailVerificationCodeExpiry.HasValue || 
                    sellerSettings.EmailVerificationCodeExpiry < DateTime.UtcNow)
                {
                    throw new ValidationException("OTP has expired. Please request a new OTP.");
                }

                // Move pending email to seller communication mail and mark as verified
                sellerSettings.CommunicationMail = sellerSettings.PendingEmail;
                sellerSettings.EmailVerified = true;
                sellerSettings.PendingEmail = null;
                sellerSettings.EmailVerificationCode = null;
                sellerSettings.EmailVerificationCodeExpiry = null;
                sellerSettings.UpdatedAt = DateTime.UtcNow;
                sellerSettings.LastUpdatedBy = currentUserId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new VerifySellerEmailOtpResponse
                {
                    Success = true,
                    EmailVerified = true,
                    Message = "Email verified successfully."
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error verifying seller email OTP");
                throw;
            }
        }
    }
}

