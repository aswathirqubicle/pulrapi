using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs
{
    public class SellerSettingsDto
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public decimal? ShippingCosts { get; set; }
        public string DeliveryTime { get; set; }
        public string ExchangePolicy { get; set; }
        public string RefundPolicy { get; set; }
    }

    public class UpdateSellerSettingsDto
    {
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Shipping costs must be a positive number.")]
        public decimal? ShippingCosts { get; set; }

        public string DeliveryTime { get; set; }
        public string ExchangePolicy { get; set; }
        public string RefundPolicy { get; set; }
    }
}

