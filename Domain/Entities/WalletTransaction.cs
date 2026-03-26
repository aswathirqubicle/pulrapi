using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class WalletTransaction : EntityBase
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        
        [Required]
        public TransactionTypeEnum TransactionType { get; set; }
        
        /// <summary>
        /// Transaction amount in major currency units (e.g. 40.00 means $40.00).
        /// Positive for credits (incoming), negative for debits (outgoing).
        /// </summary>
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; }
        
        /// <summary>
        /// Optional reference to the order that generated this transaction.
        /// </summary>
        public int? OrderId { get; set; }
        public Order Order { get; set; }

        /// <summary>
        /// Optional reference to the specific order item (OrderProductAffiliate) for refunds.
        /// </summary>
        public int? OrderProductAffiliateId { get; set; }
        public OrderProductAffiliate OrderProductAffiliate { get; set; }

        /// <summary>
        /// Description of the transaction (e.g., "Order# W564598541").
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Last 4 digits of card used for the transaction (for display purposes).
        /// </summary>
        public string? CardNumberLast4 { get; set; }
        
        /// <summary>
        /// Card type (e.g., "Visa", "Mastercard").
        /// </summary>
        public string? CardType { get; set; }
        
        /// <summary>
        /// Seller or buyer name for the transaction.
        /// </summary>
        public string? SellerName { get; set; }
        
        /// <summary>
        /// Date and time when the transaction was initiated/completed.
        /// </summary>
        [Required]
        public DateTime TransactionDate { get; set; }
        
        /// <summary>
        /// Current status of the transaction.
        /// </summary>
        [Required]
        public TransactionStatusEnum Status { get; set; }
        
        /// <summary>
        /// Collection of disputes raised for this transaction.
        /// </summary>
        public ICollection<Dispute> Disputes { get; set; }
    }
}
