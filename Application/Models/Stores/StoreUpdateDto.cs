using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Stores
{
    public class StoreUpdateDto
    {
        [Required]
        public string Uid { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LegalName { get; set; }
        [Required]
        public string UniqueName { get; set; }
        [Required]
        public string StoreEmail { get; set; }
        public bool IsEmailPublic { get; set; }
        [Required]
        public string CurrencyUid { get; set; }
        public string Description { get; set; }
        public string AffiliateId { get; set; }
    }
}
