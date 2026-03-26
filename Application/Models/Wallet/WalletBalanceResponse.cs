namespace Core.Application.Models.Wallet
{
    public class WalletBalanceResponse
    {
        public decimal AvailableBalance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
