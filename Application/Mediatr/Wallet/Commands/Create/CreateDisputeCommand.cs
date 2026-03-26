using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Extensions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Wallet;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Wallet.Commands.Create
{
    /// <summary>
    /// Command to create a dispute for a wallet transaction.
    /// </summary>
    public class CreateDisputeCommand : IRequest<DisputeResponse>
    {
        public DisputeRequest Request { get; set; }
    }

    public class CreateDisputeCommandHandler : IRequestHandler<CreateDisputeCommand, DisputeResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<CreateDisputeCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IEmailLogoService _emailLogoService;
        private readonly IConfiguration _configuration;

        public CreateDisputeCommandHandler(
            IApplicationDbContext dbContext,
            ILogger<CreateDisputeCommandHandler> logger,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IEmailLogoService emailLogoService,
            IConfiguration configuration)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _emailLogoService = emailLogoService ?? throw new ArgumentNullException(nameof(emailLogoService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<DisputeResponse> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get authenticated user
                var user = await _currentUserService.GetUserAsync(skipDetails: true);
                if (user == null)
                {
                    throw new NotAuthenticatedException("User must be logged in.");
                }

                // Get user's profile
                var profile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
                
                if (profile == null)
                {
                    throw new NotFoundException("Profile not found.");
                }

                // Validate transaction exists and belongs to the user
                var transaction = await _dbContext.WalletTransactions
                    .Include(t => t.Currency)
                    .FirstOrDefaultAsync(
                        t => t.Uid == request.Request.TransactionUid 
                        && t.ProfileId == profile.Id 
                        && t.IsActive, 
                        cancellationToken);

                if (transaction == null)
                {
                    throw new NotFoundException($"Transaction with UID '{request.Request.TransactionUid}' not found or does not belong to the current user.");
                }

                // REMOVED: Duplicate dispute check - users can now submit multiple disputes

                var dispute = new Dispute
                {
                    Uid = Guid.NewGuid().ToString(),
                    WalletTransactionId = transaction.Id,
                    ProfileId = profile.Id,
                    EmailAddress = request.Request.EmailAddress,
                    PhoneNumber = request.Request.PhoneNumber,
                    Description = request.Request.Description,
                    Status = DisputeStatusEnum.Pending,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.Id
                };


                _dbContext.Disputes.Add(dispute);
                await _dbContext.SaveChangesAsync(cancellationToken);


                _logger.LogInformation(
                    "Dispute created successfully. DisputeId: {DisputeId}, TransactionId: {TransactionId}, ProfileId: {ProfileId}, UserEmail: {UserEmail}",
                    dispute.Id, transaction.Id, profile.Id, dispute.EmailAddress);

                // Send email notifications to user and admin
                await SendDisputeNotificationEmails(dispute, transaction, profile, user);

                return new DisputeResponse
                {
                    Uid = dispute.Uid,
                    Message = "Thanks for contacting PULR Support. We've received your dispute and will reach out via the contact details you provided within 24-48 hours.",
                    SubmittedDate = dispute.CreatedDate
                };
            }
            catch (Exception ex) when (ex is not NotAuthenticatedException && ex is not NotFoundException && ex is not BadRequestException)
            {
                _logger.LogError(ex, "Error creating dispute for transaction UID {TransactionUid}: {Message}", 
                    request.Request.TransactionUid, ex.Message);
                throw;
            }
        }

        private async Task SendDisputeNotificationEmails(Dispute dispute, WalletTransaction transaction, Profile profile, User user)
        {
            try
            {
                var userName = user.FirstName.Trim() ?? user.UserName;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = "User";
                }

                var userEmailContent = GenerateUserDisputeEmailContent(
                    userName,
                    dispute.Uid,
                    transaction.Uid,
                    transaction.Amount,
                    transaction.Currency?.Code ?? "AED",
                    transaction.TransactionDate,
                    dispute.Description,
                    dispute.PhoneNumber
                );

                var adminEmailContent = GenerateAdminDisputeEmailContent(
                    userName,
                    dispute.Uid,
                    transaction.Uid,
                    transaction.Amount,
                    transaction.Currency?.Code ?? "AED",
                    transaction.TransactionDate,
                    dispute.Description,
                    dispute.PhoneNumber,
                    dispute.EmailAddress
                );

                var adminEmail = "admin@pulr.co";

                // Send email to user with inline logo attachment
                var userEmailParams = new EmailParamsDto
                {
                    To = new List<string> { dispute.EmailAddress },
                    From = _configuration["PulrEmails:Support"],
                    Subject = "We've Received Your Dispute Request",
                    Content = userEmailContent
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await userEmailParams.AddLogoAsync(_emailLogoService);
                

                await _emailService.SendMail(userEmailParams, includeAttachments: userEmailParams.Attachments.Count > 0);
                
                _logger.LogInformation(
                    "Dispute notification email sent to user. DisputeId: {DisputeId}, Email: {Email}",
                    dispute.Id, dispute.EmailAddress);

                // Send email to admin
                var adminEmailParams = new EmailParamsDto
                {
                    To = new List<string> { adminEmail },
                    From = _configuration["PulrEmails:Support"],
                    Subject = $"New Dispute #{dispute.Uid} - Action Required",
                    Content = adminEmailContent
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await adminEmailParams.AddLogoAsync(_emailLogoService);

                await _emailService.SendMail(adminEmailParams, includeAttachments: adminEmailParams.Attachments.Count > 0);
                
                _logger.LogInformation(
                    "Dispute notification email sent to admin. DisputeId: {DisputeId}, AdminEmail: {AdminEmail}",
                    dispute.Id, adminEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute notification emails for DisputeId: {DisputeId}", dispute.Id);
                // Don't throw - we don't want email failures to break the dispute creation
            }
        }

        private string GenerateUserDisputeEmailContent(
            string userName,
            string disputeUid,
            string transactionUid,
            decimal amount,
            string currency,
            DateTime transactionDate,
            string description,
            string phoneNumber)
        {
            // Extract first name from userName
            var firstName = userName.Split(' ')[0];
            
            // Use CID (Content-ID) embedding for logo - this references the inline attachment
            // SVG aspect ratio: 501x168, so for width=120, height ≈ 40px (120 * 168/501 ≈ 40)
            var logoImgTag = @"<img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR"" width=""120"" height=""40"" style=""width: 120px; height: 40px; margin-bottom: 30px; display: block; border: 0;"" />";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #ffffff;"">
    <div style=""max-width: 600px; margin: 0; padding: 20px; background-color: #ffffff;"">
        {logoImgTag}
        
        <h1 style=""font-size: 24px; font-weight: 700; color: #000; margin-top: 0; margin-bottom: 24px;"">Your dispute is under review.</h1>
        
        <p style=""margin-bottom: 16px; color: #000; font-size: 16px;""><strong>Hi {firstName},</strong></p>
        
        <p style=""margin-bottom: 16px; color: #000; font-size: 16px;""><strong>Thanks for reaching out to PULR Support.</strong></p>
        
        <p style=""margin-bottom: 16px; color: #000; font-size: 16px;"">We've received your dispute request for the transaction {transactionUid}. Our team is currently reviewing the details and will contact you using the information you provided within 24–48 hours.</p>
        
        <p style=""margin-bottom: 16px; color: #000; font-size: 16px;"">If you have any additional information to share, please reply to this email.</p>
        
        <p style=""margin-bottom: 16px; color: #000; font-size: 16px;"">Thanks for your patience,<br>
        <strong>PULR Support Team</strong></p>
        
        <div style=""margin-top: 40px; padding-top: 30px; border-top: 1px solid #e5e5e5; text-align: left; font-size: 14px; color: #333;"">
            <p style=""margin: 4px 0; font-size: 14px;""><strong>Thanks for shopping on PULR 💜</strong></p>
            <p style=""margin: 4px 0; font-size: 14px;"">Discover, tag, and shop — all in one place.</p>
            
            <div style=""font-size: 12px; color: #666; margin-top: 20px;"">
                <p style=""margin: 4px 0;"">To ensure you receive these emails in your inbox, please add support@pulr.co to your address book.</p>
                <p style=""margin: 4px 0;"">© 2025 PULR. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateAdminDisputeEmailContent(
            string userName,
            string disputeUid,
            string transactionUid,
            decimal amount,
            string currency,
            DateTime transactionDate,
            string description,
            string phoneNumber,
            string userEmail)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f44336 0%, #e91e63 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alert-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 8px; }}
        .info-box {{ background: white; padding: 20px; margin: 20px 0; border-radius: 8px; border-left: 4px solid #f44336; }}
        .info-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #f44336; }}
        .action-btn {{ display: inline-block; background: #f44336; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR"" width=""120"" height=""40"" style=""width: 120px; height: 40px; margin-bottom: 20px; display: block; border: 0;"" />
            <h1>⚠️ New Dispute Alert</h1>
            <p>Action Required - Review Needed</p>
        </div>
        <div class=""content"">
            <div class=""alert-box"">
                <strong>⚠️ A new dispute has been submitted and requires your attention.</strong>
            </div>

            <h3 style=""color: #f44336;"">Dispute Information</h3>
            
            <div class=""info-box"">
                <h4 style=""margin-top: 0; color: #f44336;"">Dispute Details</h4>
                <div class=""info-row"">
                    <span class=""label"">Dispute ID:</span> {disputeUid}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Transaction ID:</span> {transactionUid}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Amount:</span> {amount:F2} {currency}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Transaction Date:</span> {transactionDate:dd MMM yyyy, HH:mm}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Submitted:</span> {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC
                </div>
            </div>

            <div class=""info-box"">
                <h4 style=""margin-top: 0; color: #f44336;"">User Information</h4>
                <div class=""info-row"">
                    <span class=""label"">Name:</span> {userName}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Email:</span> {userEmail}
                </div>
                <div class=""info-row"">
                    <span class=""label"">Phone:</span> {phoneNumber}
                </div>
            </div>

            <div class=""info-box"">
                <h4 style=""margin-top: 0; color: #f44336;"">Issue Description</h4>
                <p style=""margin: 0; padding: 10px; background: #f8f9fa; border-radius: 4px;"">{description}</p>
            </div>

            <h3 style=""color: #f44336;"">Required Actions</h3>
            <ul>
                <li><strong>Review the dispute</strong> within 24-48 hours</li>
                <li><strong>Contact the user</strong> at {userEmail} or {phoneNumber}</li>
                <li><strong>Investigate the transaction</strong> ID: {transactionUid}</li>
                <li><strong>Update dispute status</strong> in the admin panel</li>
                <li><strong>Provide resolution</strong> and communicate with the user</li>
            </ul>

            <p style=""margin-top: 30px; text-align: center;"">
                <strong>Please handle this dispute promptly to maintain customer satisfaction.</strong>
            </p>
        </div>
        <div class=""footer"">
            <p>This is an automated notification from PULR Admin System.</p>
            <p>© 2026 PULR. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
