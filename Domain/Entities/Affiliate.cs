
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class Affiliate : EntityBase
    {
        [Required]
        public string AffiliateId { get; set; }
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        public OrderProductAffiliate OrderProductAffiliate { get; set; }
    }
}
