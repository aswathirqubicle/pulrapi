using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class UserBagProduct : EntityBase
    {
        [Required]
        public int BagProductId { get; set; }
        public Product BagProduct { get; set; }
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        public int Quantity { get; set; }
        
        // Store the variant combination UID to preserve size/color selection
        public string ProductVariantCombinationUid { get; set; }
        public ProductVariantCombination ProductVariantCombination { get; set; }
    }
}
