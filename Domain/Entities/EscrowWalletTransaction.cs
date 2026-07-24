using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class EscrowWalletTransaction : EntityBase
    {
        [Required]
        public int EscrowWalletId { get; set; }
        public EscrowWallet EscrowWallet { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int OrderProductAffiliateId { get; set; }
        public OrderProductAffiliate OrderProductAffiliate { get; set; }

        [Required]
        public EscrowWalletTransactionTypeEnum TransactionType { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public decimal? SellerAmount { get; set; }
        public decimal? CreatorAmount { get; set; }
        public decimal? PlatformAmount { get; set; }

        public bool IsCollabSale { get; set; } = false;

        public string CreatorUserId { get; set; }

        public DateTime? EscrowReleaseAt { get; set; }
        public DateTime? ReleasedAt { get; set; }

        public EscrowWalletTransactionStatusEnum Status { get; set; } = EscrowWalletTransactionStatusEnum.Active;

        [Required]
        public string StripePaymentIntentId { get; set; }

        public string StripeTransferIdSeller { get; set; }
        public string StripeTransferIdCreator { get; set; }
        public string StripeRefundId { get; set; }
    }
}