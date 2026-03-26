using System;

namespace Core.Application.Models.Wallet
{
    /// <summary>
    /// Response model returned after successfully creating a dispute.
    /// </summary>
    public class DisputeResponse
    {
        /// <summary>
        /// Unique identifier for the dispute.
        /// </summary>
        public string Uid { get; set; }
        
        /// <summary>
        /// Confirmation message to display to the user.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Date and time when the dispute was submitted.
        /// </summary>
        public DateTime SubmittedDate { get; set; }
    }
}
