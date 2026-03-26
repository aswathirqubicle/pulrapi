
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class GlobalCurrencySetting : EntityBase
    {
        public int BaseCurrencyId { get; set; }
        public Currency BaseCurrency { get; set; }

        [Required]
        public DateTime ExchangeRateLastUpdateUtc { get; set; }
        [Required]
        public DateTime ExchangeRateNextUpdateUtc { get; set; }

        public virtual ICollection<ExchangeRate> ExchangeRates { get; set; }
    }
}
