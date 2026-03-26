using System.ComponentModel.DataAnnotations.Schema;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ExchangeRate : EntityBase
    {

        // BaseCurrency price * Rate = Currency price
        public int GlobalCurrencySettingId { get; set; }
        public GlobalCurrencySetting GlobalCurrencySetting { get; set; }

        public int? CurrencyId { get; set; }
        public Currency Currency { get; set; }

        [Column(TypeName = "decimal(10, 4)")]
        public decimal Rate { get; set; }
    }
}
