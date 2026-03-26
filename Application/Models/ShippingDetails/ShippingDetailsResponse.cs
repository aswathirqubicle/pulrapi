using System.Collections.Generic;
using Core.Application.Mappings;

namespace Core.Application.Models.ShippingDetails
{
    public class ShippingDetailsResponse : IMapFrom<Domain.Entities.ShippingDetails>
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AddressDetails { get; set; } = string.Empty; // Apt #, Floor #, Landmark
        
        // Individual address fields
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsBillingAddress { get; set; }


        public static ShippingDetailsResponse? MapFromEntity(Domain.Entities.ShippingDetails? src)
        {
            if (src == null) return null;

            return new ShippingDetailsResponse
            {
                Uid = src.Uid,
                Email = src.Email,
                AddressDetails = src.Apartment ?? string.Empty,
                Street = src.Address ?? string.Empty,
                City = src.City ?? string.Empty,
                PostalCode = src.ZipCode ?? string.Empty,
                Country = src.Country,
                CountryCode = src.CountryNavigation?.Iso2 ?? string.Empty,
                PhoneNumber = src.PhoneNumber,
                IsDefault = src.DefaultShippingAddress,
                IsBillingAddress = src.IsBillingAddress
            };
        }
    }
}
