using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Currencies;
using Core.Infrastructure.Services;
using Core.Domain.Entities;

namespace Core.Infrastructure.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExchangeRateService> _logger;
        private readonly IHttpClientService _httpClientService;
        private readonly IApplicationDbContext _dbContext;

        public ExchangeRateService(IConfiguration configuration,
            ILogger<ExchangeRateService> logger,
            IHttpClientService httpClientService,
            IApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientService = httpClientService;
            _dbContext = dbContext;
        }


        public async Task<List<ExchangeRate>> GetExchangeRates(List<string> currencyCodes = null)
        {
            if (currencyCodes == null || currencyCodes.Count() == 0)
                return await _dbContext.ExchangeRates
                    .AsSplitQuery()
                    .Include(er => er.Currency)
                    .ToListAsync();

            var x = await _dbContext.ExchangeRates
                .AsSplitQuery()
                .Include(er => er.Currency)
                .Where(er => currencyCodes.Contains(er.Currency.Code)).ToListAsync();
            return x;
        }

        public double GetCurrencyExchangeRates(string currencyCodeFrom, string currencyCodeTo, double? price, List<ExchangeRate> exchangeRates)
        {
            if (currencyCodeFrom == null || currencyCodeTo == null)
            {
                return (double)price;
            }

            decimal? currencyFromRate = exchangeRates.Where(er =>  er.Currency?.Code == currencyCodeFrom).Select(er => er.Rate).SingleOrDefault();
            decimal? currencyToRate = exchangeRates.Where(er => er.Currency?.Code == currencyCodeTo).Select(er => er.Rate).SingleOrDefault();

            if (currencyFromRate == null || currencyToRate == null)
            {
                throw new ArgumentException("CurrencyFrom rate or currencyToRate is null");
            }

            return Math.Round((double)(((decimal)price / currencyFromRate) * currencyToRate), 2);
        }


        public async Task GetExchangeRates()
        {
            try
            {
                // baseCurrencyCode e.g. -> EUR
                var globalCurrencySettings = await _dbContext.GlobalCurrencySettings.Include(gcs => gcs.BaseCurrency).SingleOrDefaultAsync();
                if (globalCurrencySettings == null)
                {
                    _logger.LogError("GlobalCurrencySettings null");
                    throw new Exception("GlobalCurrencySettings null");
                }

                var apiKey = _configuration["ExchangeRate:ApiKey"];
                var exchangeRateUrl = _configuration["ExchangeRate:Url"];
                exchangeRateUrl = exchangeRateUrl.Replace("YOUR-API-KEY", apiKey).Replace("CURRENCY-CODE", globalCurrencySettings.BaseCurrency.Code);

                var result = await _httpClientService.CreateRequest(HttpMethod.Get, exchangeRateUrl, null, null);
                var resultAsString = await result.Content.ReadAsStringAsync();
                var exchangeRateResponse = JsonConvert.DeserializeObject<ExchangeRateResponse>(resultAsString);

                globalCurrencySettings.ExchangeRateLastUpdateUtc = exchangeRateResponse.time_last_update_utc;
                globalCurrencySettings.ExchangeRateNextUpdateUtc = exchangeRateResponse.time_next_update_utc;

                foreach (JProperty parsedProperty in exchangeRateResponse.conversion_rates.Properties())
                {
                    string propertyName = parsedProperty.Name;
                    decimal propertyValue = (decimal)parsedProperty.Value;
                    exchangeRateResponse.conversionRates.Add(new KeyValuePair<string, decimal>(propertyName, propertyValue));
                }

                if (exchangeRateResponse.result.ToLower() != "success")
                {

                    _logger.LogError("GetExchangeRates Error {exchangeRateResponse}", exchangeRateResponse);
                    throw new Exception("GetExchangeRates Error");
                }

                var exchangeRates = await _dbContext.ExchangeRates.Where(er => er.IsActive).ToListAsync();
                _dbContext.ExchangeRates.RemoveRange(exchangeRates);

                foreach (var item in exchangeRateResponse.conversionRates)
                {
                    _dbContext.ExchangeRates.Add(new ExchangeRate()
                    {
                        GlobalCurrencySetting = globalCurrencySettings,
                        Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == item.Key.Trim().ToUpper()),
                        Rate = item.Value,
                    });
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

    }
}
