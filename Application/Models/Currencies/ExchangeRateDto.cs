namespace Core.Application.Models.Currencies
{
    public class ExchangeRateDto
    {
        public string BaseCurrencyCode { get; set; }
        public decimal Rate { get; set; }
        public string CurrencyCode { get; set; }
    }
}
