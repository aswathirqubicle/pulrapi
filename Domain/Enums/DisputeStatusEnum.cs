namespace Core.Domain.Enums
{
    /// <summary>
    /// Represents the status of a dispute raised by a user.
    /// </summary>
    public enum DisputeStatusEnum
    {
        /// <summary>
        /// Dispute has been submitted and is awaiting review.
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// Dispute is currently being reviewed by support team.
        /// </summary>
        UnderReview = 1,
        
        /// <summary>
        /// Dispute has been resolved in favor of the user.
        /// </summary>
        Resolved = 2,
        
        /// <summary>
        /// Dispute has been rejected.
        /// </summary>
        Rejected = 3
    }
}
