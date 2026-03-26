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

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetSellerSettingsQuery : IRequest<SellerSettingsDto>
    {
    }

    public class GetSellerSettingsQueryHandler : IRequestHandler<GetSellerSettingsQuery, SellerSettingsDto>
    {
        private readonly ILogger<GetSellerSettingsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetSellerSettingsQueryHandler(
            ILogger<GetSellerSettingsQueryHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<SellerSettingsDto> Handle(GetSellerSettingsQuery request, CancellationToken cancellationToken)
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

                // Helper lambda to determine email to display
                Func<string> getDisplayEmail = () => {
                    if (sellerSettings != null && !string.IsNullOrEmpty(sellerSettings.CommunicationMail))
                    {
                        return sellerSettings.CommunicationMail;
                    }
                    // Fallback to user primary email if communication mail is not set in seller settings
                    return user.Email;
                };

                if (sellerSettings == null)
                {
                    // Return default values if settings don't exist yet
                    return new SellerSettingsDto
                    {
                        PhoneNumber = null,
                        Email = getDisplayEmail(),
                        EmailVerified = false, // Seller email itself isn't verified yet
                        ShippingCosts = null,
                        DeliveryTime = null,
                        ExchangePolicy = null,
                        RefundPolicy = null
                    };
                }

                return new SellerSettingsDto
                {
                    PhoneNumber = sellerSettings.PhoneNumber,
                    Email = getDisplayEmail(),
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

