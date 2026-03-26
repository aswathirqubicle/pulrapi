namespace Core.Domain.Entities
{
    public class ShippingDetails : EntityBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Floor { get; set; }
        public string Apartment { get; set; }
        public string Country { get; set; }
        public string CountryUid { get; set; }
        public Country CountryNavigation { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public bool DefaultShippingAddress { get; set; }
        public bool IsBillingAddress { get; set; }
        public string? UserId { get; set; }
        public User User { get; set; }
    }
}
