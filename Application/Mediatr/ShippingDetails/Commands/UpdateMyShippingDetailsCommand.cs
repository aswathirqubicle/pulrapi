using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Commands;
using Core.Application.Models.ShippingDetails;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Mediatr.ShippingDetails.Commands
{
    public class UpdateMyShippingDetailsCommand : IRequest<ShippingDetailsResponse>
    {
        [Required] public string Uid { get; set; }
        public string StreetAddress { get; set; }
        public string AddressDetails { get; set; } // Apt #, Floor #, Landmark
        public string City { get; set; }
        public string Country { get; set; }
        [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
        public string CountryUid { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public bool? DefaultShippingAddress { get; set; }
        public bool? IsBillingAddress { get; set; }
    }

    public class UpdateMyShippingDetailsCommandHandler : IRequestHandler<UpdateMyShippingDetailsCommand, ShippingDetailsResponse>
    {
        private readonly ILogger<UpdateMyShippingDetailsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UpdateMyShippingDetailsCommandHandler(ILogger<UpdateMyShippingDetailsCommandHandler> logger,
            IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<ShippingDetailsResponse> Handle(UpdateMyShippingDetailsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser == null)
                {
                    throw new NotAuthenticatedException("");
                }

                var existingShippingDetails = await _dbContext.ShippingDetails
                    .Include(sd => sd.CountryNavigation)
                    .SingleOrDefaultAsync(sd => sd.IsActive && sd.UserId == cUser.Id && sd.Uid == request.Uid, cancellationToken);
                
                if (existingShippingDetails == null)
                {
                    throw new NotFoundException("Shipping address not found");
                }

                // Handle DefaultShippingAddress if provided
                if (request.DefaultShippingAddress.HasValue && request.DefaultShippingAddress.Value)
                {
                    var isBilling = request.IsBillingAddress ?? existingShippingDetails.IsBillingAddress;
                    var existingAddresses = await _dbContext.ShippingDetails
                        .Where(sd => sd.IsActive && sd.Uid != request.Uid && sd.DefaultShippingAddress && sd.User == cUser && sd.IsBillingAddress == isBilling)
                        .ToListAsync(cancellationToken);
                    
                    foreach (var addr in existingAddresses)
                    {
                        addr.DefaultShippingAddress = false;
                    }
                    existingShippingDetails.DefaultShippingAddress = true;
                }
                else if (request.DefaultShippingAddress.HasValue && !request.DefaultShippingAddress.Value)
                {
                    existingShippingDetails.DefaultShippingAddress = false;
                }

                // Parse AddressDetails to extract Apartment and Floor if present
                if (!string.IsNullOrWhiteSpace(request.AddressDetails))
                {
                    string apartment = null;
                    string floor = null;
                    
                    // Parse AddressDetails format: "Apt #, Floor #, Landmark" or similar
                    var details = request.AddressDetails.Split(',').Select(d => d.Trim()).ToList();
                    foreach (var detail in details)
                    {
                        if (detail.StartsWith("Apt", System.StringComparison.OrdinalIgnoreCase))
                        {
                            apartment = detail.Replace("Apt", "", System.StringComparison.OrdinalIgnoreCase).Trim();
                        }
                        else if (detail.StartsWith("Floor", System.StringComparison.OrdinalIgnoreCase))
                        {
                            floor = detail.Replace("Floor", "", System.StringComparison.OrdinalIgnoreCase).Trim();
                        }
                    }
                    
                    // If no structured format found, store the whole AddressDetails in Apartment field
                    if (string.IsNullOrWhiteSpace(apartment) && string.IsNullOrWhiteSpace(floor))
                    {
                        apartment = request.AddressDetails;
                    }

                    existingShippingDetails.Floor = floor;
                    existingShippingDetails.Apartment = apartment ?? request.AddressDetails;
                }

                // Update only provided fields (partial update)
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    existingShippingDetails.PhoneNumber = request.PhoneNumber;
                }
                
                if (!string.IsNullOrWhiteSpace(request.StreetAddress))
                {
                    existingShippingDetails.Address = request.StreetAddress;
                }
                
                if (!string.IsNullOrWhiteSpace(request.City))
                {
                    existingShippingDetails.City = request.City;
                }
                
                if (!string.IsNullOrWhiteSpace(request.Country))
                {
                    existingShippingDetails.Country = request.Country;
                }
                
                if (!string.IsNullOrWhiteSpace(request.CountryUid))
                {
                    existingShippingDetails.CountryUid = request.CountryUid;
                }
                
                if (!string.IsNullOrWhiteSpace(request.ZipCode))
                {
                    existingShippingDetails.ZipCode = request.ZipCode;
                }
                
                if (request.IsBillingAddress.HasValue)
                {
                    existingShippingDetails.IsBillingAddress = request.IsBillingAddress.Value;
                }

                // Always update FirstName, LastName, and Email from user profile
                existingShippingDetails.FirstName = cUser.FirstName;
                existingShippingDetails.LastName = cUser.LastName;
                existingShippingDetails.Email = cUser.Email;
                
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Reload to include CountryNavigation for CountryCode
                var result = await _dbContext.ShippingDetails
                    .Include(sd => sd.CountryNavigation)
                    .FirstOrDefaultAsync(sd => sd.Uid == existingShippingDetails.Uid, cancellationToken);

                return ShippingDetailsResponse.MapFromEntity(result ?? existingShippingDetails);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
