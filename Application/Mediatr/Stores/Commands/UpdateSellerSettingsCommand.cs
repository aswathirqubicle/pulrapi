using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.DTOs;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class UpdateSellerSettingsCommand : IRequest<SellerSettingsDto>
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public decimal? ShippingCosts { get; set; }
        public string DeliveryTime { get; set; }
        public string ExchangePolicy { get; set; }
        public string RefundPolicy { get; set; }
    }

    public class UpdateSellerSettingsCommandHandler : IRequestHandler<UpdateSellerSettingsCommand, SellerSettingsDto>
    {
        private readonly ILogger<UpdateSellerSettingsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UpdateSellerSettingsCommandHandler(
            ILogger<UpdateSellerSettingsCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<SellerSettingsDto> Handle(UpdateSellerSettingsCommand request, CancellationToken cancellationToken)
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

                var sellerSettings = await _dbContext.SellerSettings
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                // If user is trying to set an email, check if it's verified
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    // Check if this email is verified in seller settings
                    bool isEmailVerified = sellerSettings != null && 
                                          (sellerSettings.CommunicationMail == request.Email || sellerSettings.EmailVerified);

                    if (!isEmailVerified)
                    {
                        throw new ValidationException("Email must be verified before it can be saved. Please verify your email using the OTP verification process.");
                    }
                }

                if (sellerSettings == null)
                {
                    // Create new seller settings if they don't exist
                    // Note: Email should already be verified via OTP before reaching here
                    sellerSettings = new SellerSettings
                    {
                        UserId = currentUserId,
                        PhoneNumber = request.PhoneNumber,
                        CommunicationMail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
                        EmailVerified = !string.IsNullOrWhiteSpace(request.Email), // Should already be verified
                        ShippingCosts = request.ShippingCosts,
                        DeliveryTime = request.DeliveryTime,
                        ExchangePolicy = request.ExchangePolicy,
                        RefundPolicy = request.RefundPolicy,
                        CreatedBy = currentUserId,
                        UpdatedAt = DateTime.UtcNow,
                        LastUpdatedBy = currentUserId
                    };
                    _dbContext.SellerSettings.Add(sellerSettings);
                }
                else
                {
                    // Update existing settings - allow null/empty to clear fields
                    if (request.PhoneNumber != null)
                    {
                        sellerSettings.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;
                    }
                    if (request.Email != null)
                    {
                        // Update SellerSettings.CommunicationMail
                        sellerSettings.CommunicationMail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email;
                    }
                    if (request.ShippingCosts.HasValue)
                    {
                        sellerSettings.ShippingCosts = request.ShippingCosts;
                    }
                    if (request.DeliveryTime != null)
                    {
                        sellerSettings.DeliveryTime = string.IsNullOrWhiteSpace(request.DeliveryTime) ? null : request.DeliveryTime;
                    }
                    if (request.ExchangePolicy != null)
                    {
                        sellerSettings.ExchangePolicy = string.IsNullOrWhiteSpace(request.ExchangePolicy) ? null : request.ExchangePolicy;
                    }
                    if (request.RefundPolicy != null)
                    {
                        sellerSettings.RefundPolicy = string.IsNullOrWhiteSpace(request.RefundPolicy) ? null : request.RefundPolicy;
                    }
                    sellerSettings.UpdatedAt = DateTime.UtcNow;
                    sellerSettings.LastUpdatedBy = currentUserId;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new SellerSettingsDto
                {
                    PhoneNumber = sellerSettings.PhoneNumber,
                    Email = sellerSettings.CommunicationMail,
                    EmailVerified = sellerSettings.EmailVerified,
                    ShippingCosts = sellerSettings.ShippingCosts,
                    DeliveryTime = sellerSettings.DeliveryTime,
                    ExchangePolicy = sellerSettings.ExchangePolicy,
                    RefundPolicy = sellerSettings.RefundPolicy
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}

