using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class SellerSettings : EntityBase
    {
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }

        public string PhoneNumber { get; set; }
        public string CommunicationMail { get; set; }
        public bool EmailVerified { get; set; }
        public string PendingEmail { get; set; } // Email awaiting verification
        public string EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        public decimal? ShippingCosts { get; set; }
        public string DeliveryTime { get; set; }
        public string ExchangePolicy { get; set; }
        public string RefundPolicy { get; set; }
    }
}

