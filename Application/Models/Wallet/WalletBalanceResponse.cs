namespace Core.Application.Models.Wallet
{
    public class WalletBalanceResponse
    {
        public decimal AvailableBalance { get; set; }
        public decimal EscrowBalance { get; set; }   // In-escrow / locked (not yet withdrawable)
        public decimal TotalBalance { get; set; }     // AvailableBalance + EscrowBalance
        public string CurrencyCode { get; set; }
    }
}
