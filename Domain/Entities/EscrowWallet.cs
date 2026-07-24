using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class EscrowWallet : EntityBase
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        public decimal LockedBalance { get; set; } = 0;

        [Required]
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; }

        public virtual ICollection<EscrowWalletTransaction> EscrowWalletTransactions { get; set; }
    }
}