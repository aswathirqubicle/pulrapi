using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IExchangeRateService
    {
        Task GetExchangeRates();
        Task<List<ExchangeRate>> GetExchangeRates(List<string> currencyCodes = null);
        double GetCurrencyExchangeRates(string currencyCodeFrom, string currencyCodeTo, double? price, List<ExchangeRate> exchangeRates);
    }
}
