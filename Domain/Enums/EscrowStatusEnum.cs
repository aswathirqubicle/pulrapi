namespace Core.Domain.Enums
{
    public enum EscrowStatusEnum
    {
        PendingDelivery = 1,
        InEscrow = 2,
        Released = 3,
        Refunded = 4,
        RefundInProgress = 5,
        Disputed = 6,
        Cancelled = 7,
        RefundRequested = 8,
        RefundRejected = 9
    }
}