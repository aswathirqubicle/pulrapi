using System.Collections.Generic;
using Core.Application.Models.Currencies;

namespace Core.Application.Models.Currencies
{
    public class AllCurrenciesResponse
    {
        public CurrencyDetailsResponse MainCurrency { get; set; }
        public List<CurrencyDetailsResponse> Currencies { get; set; }
    }
}
