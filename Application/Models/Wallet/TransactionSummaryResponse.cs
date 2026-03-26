using System;

namespace Core.Application.Models.Wallet
{
    public class TransactionSummaryResponse
    {
        public string Uid { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public string CardUsed { get; set; }
        public string CardNumber { get; set; }
        public DateTime InitiationDate { get; set; }
        public string OrderNumber { get; set; }
        public string SellerName { get; set; }
    }
}
