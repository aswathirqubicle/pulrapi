using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Wallet
{
    /// <summary>
    /// Request model for creating a dispute on a wallet transaction.
    /// </summary>
    public class DisputeRequest
    {
        /// <summary>
        /// The UID of the wallet transaction being disputed.
        /// </summary>
        [Required(ErrorMessage = "Transaction UID is required")]
        public string TransactionUid { get; set; }
        
        /// <summary>
        /// Contact email address for dispute follow-up.
        /// </summary>
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(255)]
        public string EmailAddress { get; set; }
        
        /// <summary>
        /// Contact phone number for dispute follow-up.
        /// </summary>
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(50)]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// Brief description of the issue/reason for the dispute.
        /// </summary>
        [Required(ErrorMessage = "Description is required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }
    }
}
