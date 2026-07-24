namespace Core.Application.Models.Settings
{
    public class PaymentFeeSettingResponse
    {
        public string Uid { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal FeePercentage { get; set; }
        public decimal FixedFee { get; set; }
    }

    public class PlatformSettingResponse
    {
        public string Uid { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal VatRate { get; set; }
        public decimal PlatformFeePercentage { get; set; }
        public decimal DirectSaleSellerPercentage { get; set; }
        public decimal CollabSaleSellerPercentage { get; set; }
        public decimal CollabSaleCreatorPercentage { get; set; }
        public decimal MinimumWithdrawalAmount { get; set; }
        public int DeliveryExtensionHours { get; set; }
        public int RefundWindowDays { get; set; }
        public int ExchangeWindowDays { get; set; }
        public int EscrowHoldDays { get; set; }
    }
}