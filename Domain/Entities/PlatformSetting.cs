using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class PlatformSetting : EntityBase
    {
        [Required]
        public decimal CommissionRate { get; set; }

        [Required]
        public decimal VatRate { get; set; }

        [Required]
        public decimal PlatformFeePercentage { get; set; }

        [Required]
        public decimal DirectSaleSellerPercentage { get; set; }

        [Required]
        public decimal CollabSaleSellerPercentage { get; set; }

        [Required]
        public decimal CollabSaleCreatorPercentage { get; set; }

        [Required]
        public decimal MinimumWithdrawalAmount { get; set; }

        [Required]
        public int DeliveryExtensionHours { get; set; }

        [Required]
        public int RefundWindowDays { get; set; }

        [Required]
        public int ExchangeWindowDays { get; set; }

        [Required]
        public int EscrowHoldDays { get; set; }

        [Required]
        public int RefundResponseDays { get; set; } = 7;  // Days seller has to respond before auto-escalation
    }
}
