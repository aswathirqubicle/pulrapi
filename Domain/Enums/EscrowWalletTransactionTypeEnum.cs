namespace Core.Domain.Enums
{
    public enum EscrowWalletTransactionTypeEnum
    {
        Lock = 1,
        Release = 2,
        RefundReversal = 3,
        DisputeHold = 4
    }

    public enum EscrowWalletTransactionStatusEnum
    {
        Active = 1,
        Released = 2,
        Refunded = 3,
        Disputed = 4,
        Paused = 5
    }
}