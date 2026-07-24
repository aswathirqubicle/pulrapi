using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class PaymentFeeSetting : EntityBase
    {
        [Required]
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; }

        [Required]
        public decimal FeePercentage { get; set; }

        [Required]
        public decimal FixedFee { get; set; }
    }
}