using System;
using System.Collections.Generic;

namespace Core.Application.Models.Wallet
{
    public class WalletTransactionResponse
    {
        public string Uid { get; set; }
        public string OrderUid { get; set; }
        public string OrderItemUid { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? PlacementDate { get; set; }
        public string Status { get; set; }
        public string CardNumberLast4 { get; set; }
        public string CardType { get; set; }
        public List<string> SellerNames { get; set; }
    }
}
