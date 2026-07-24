using System.Collections.Generic;

namespace Core.Application.Models.Stripe
{
    public class RefundRequest
    {
        public string PaymentIntentId { get; set; }
        public long? AmountInCents { get; set; }
        public string Reason { get; set; } = "requested_by_customer";
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class RefundResponse
    {
        public string RefundId { get; set; }
        public string PaymentIntentId { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }

    public class ReverseTransferRequest
    {
        public string TransferId { get; set; }
        public long? AmountInCents { get; set; }
        public string Description { get; set; }
    }

    public class TransferReversalResponse
    {
        public string ReversalId { get; set; }
        public string TransferId { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
    }
}