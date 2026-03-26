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

namespace Core.Application.Mediatr.ShippingDetails.Commands;

public class CreateShippingAddressCommand : IRequest<ShippingDetailsResponse>
{
    [Required] public string StreetAddress { get; set; }
    [Required] public string AddressDetails { get; set; } // Apt #, Floor #, Landmark
    [Required] public string City { get; set; }
    public string Country { get; set; }
    [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
    public string CountryUid { get; set; }
    public string ZipCode { get; set; }
    [Required] public string PhoneNumber { get; set; }
    [Required] public bool DefaultShippingAddress { get; set; }
    public bool IsBillingAddress { get; set; }
}

public class CreateShippingAddressCommandHandler : IRequestHandler<CreateShippingAddressCommand, ShippingDetailsResponse>
{
    private readonly ILogger<CreateShippingAddressCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateShippingAddressCommandHandler(
        ILogger<CreateShippingAddressCommandHandler> logger,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

        public async Task<ShippingDetailsResponse> Handle(CreateShippingAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
                // We only need basic user identity here; skip heavy profile/roles loading
                var cUser = await _currentUserService.GetUserAsync(skipDetails: true);
            if (cUser == null)
            {
                throw new NotAuthenticatedException("");
            }

                if (request.DefaultShippingAddress)
                {
                    var existingAddresses = await _dbContext.ShippingDetails
                        .Where(sd => sd.IsActive && sd.DefaultShippingAddress && sd.UserId == cUser.Id && sd.IsBillingAddress == request.IsBillingAddress)
                        .ToListAsync(cancellationToken);

                    foreach (var addr in existingAddresses)
                    {
                        addr.DefaultShippingAddress = false;
                    }
                }

            // Parse AddressDetails to extract Apartment and Floor if present
            string apartment = null;
            string floor = null;
            
            if (!string.IsNullOrWhiteSpace(request.AddressDetails))
            {
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
            }

            // Populate FirstName, LastName, and Email from user profile
                var shippingDetail = new Domain.Entities.ShippingDetails
                {
                    FirstName = cUser.FirstName,
                    LastName = cUser.LastName,
                    Email = cUser.Email,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.StreetAddress,
                    Floor = floor,
                    Apartment = apartment ?? request.AddressDetails,
                    City = request.City,
                    Country = request.Country,
                    CountryUid = request.CountryUid,
                    ZipCode = request.ZipCode,
                    DefaultShippingAddress = request.DefaultShippingAddress,
                    IsBillingAddress = request.IsBillingAddress,
                    UserId = cUser.Id   // use explicit FK, avoid shadow UserId2
                };
            _dbContext.ShippingDetails.Add(shippingDetail);

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            // Reload to include CountryNavigation for CountryCode
            var result = await _dbContext.ShippingDetails
                .Include(sd => sd.CountryNavigation)
                .FirstOrDefaultAsync(sd => sd.Uid == shippingDetail.Uid, cancellationToken);

            return ShippingDetailsResponse.MapFromEntity(result ?? shippingDetail)!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
