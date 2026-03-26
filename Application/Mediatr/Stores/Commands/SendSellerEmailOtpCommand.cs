using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Extensions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ValidationException = Core.Application.Exceptions.ValidationException;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class SendSellerEmailOtpCommand : IRequest<SendSellerEmailOtpResponse>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class SendSellerEmailOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class SendSellerEmailOtpCommandHandler : IRequestHandler<SendSellerEmailOtpCommand, SendSellerEmailOtpResponse>
    {
        private readonly ILogger<SendSellerEmailOtpCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IEmailLogoService _emailLogoService;
        private readonly IConfiguration _configuration;

        public SendSellerEmailOtpCommandHandler(
            ILogger<SendSellerEmailOtpCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IEmailLogoService emailLogoService,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _emailLogoService = emailLogoService ?? throw new ArgumentNullException(nameof(emailLogoService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<SendSellerEmailOtpResponse> Handle(SendSellerEmailOtpCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }

                // Get or create seller settings
                var sellerSettings = await _dbContext.SellerSettings
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                if (sellerSettings == null)
                {
                    sellerSettings = new SellerSettings
                    {
                        UserId = currentUserId,
                        CreatedBy = currentUserId,
                        UpdatedAt = DateTime.UtcNow,
                        LastUpdatedBy = currentUserId
                    };
                    _dbContext.SellerSettings.Add(sellerSettings);
                }

                // Removed check for email already in use by another verified seller as per user request
                // This allows multiple seller accounts to potentially verify/use the same email address

                // Generate 6-digit OTP
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();
                
                // Store email in PendingEmail field (not Email) until verified
                sellerSettings.PendingEmail = request.Email;
                sellerSettings.EmailVerificationCode = code;
                sellerSettings.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15); // 15 min expiry
                sellerSettings.UpdatedAt = DateTime.UtcNow;
                sellerSettings.LastUpdatedBy = currentUserId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Send email with OTP
                var emailContent = $@"
<div style=""font-family: Arial, sans-serif; text-align: center; background: #fff; padding: 32px;"">
  <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
  <h2>Verify your seller email address</h2>
  <p>Use the following code to verify your seller email address:</p>
  <div style=""font-size: 2em; font-weight: bold; letter-spacing: 8px; margin: 24px 0;"">{code}</div>
  <p>This code will expire in 15 minutes.</p>
  <p>If you didn't request this verification, you can ignore this email.</p>
</div>
";

                var emailParams = new EmailParamsDto()
                {
                    From = _configuration["PulrEmails:Support"],
                    Subject = "Verify your seller email address on Pulr.co",
                    Content = emailContent,
                    To = new List<string>() { request.Email },
                    IsTemplateFromFile = false,
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await emailParams.AddLogoAsync(_emailLogoService);

                await _emailService.SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);

                return new SendSellerEmailOtpResponse
                {
                    Success = true,
                    Message = "OTP sent successfully to your email address."
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending seller email OTP");
                throw;
            }
        }
    }
}

