using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    /// <summary>
    /// Represents a dispute raised by a user for a wallet transaction.
    /// </summary>
    public class Dispute : EntityBase
    {
        /// <summary>
        /// The wallet transaction being disputed.
        /// </summary>
        [Required]
        public int WalletTransactionId { get; set; }
        public WalletTransaction WalletTransaction { get; set; }
        
        /// <summary>
        /// The profile/user who raised the dispute.
        /// </summary>
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        
        /// <summary>
        /// Contact email address provided by the user for dispute follow-up.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string EmailAddress { get; set; }
        
        /// <summary>
        /// Contact phone number provided by the user for dispute follow-up.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// Brief description of the issue/reason for the dispute.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }
        
        /// <summary>
        /// Current status of the dispute.
        /// </summary>
        [Required]
        public DisputeStatusEnum Status { get; set; }
        
        /// <summary>
        /// Date and time when the dispute was created.
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// Date and time when the dispute was last updated.
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
        
        /// <summary>
        /// Optional notes from support team regarding the dispute resolution.
        /// </summary>
        [MaxLength(2000)]
        public string? ResolutionNotes { get; set; }
    }
}
