namespace Core.Domain.Enums
{
    public enum OrderStatusEnum
    {
        Pending,
        Rejected,
        Processing,
        Shipped,
        Delivered,
        Completed,
        OrderFailed,    // Countdown expired, awaiting buyer action
        Refunded        // Buyer chose refund, order closed
    }
}
