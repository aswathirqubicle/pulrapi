
namespace Core.Application.Models.Stores
{
    public class StoreDetailsDto
    {
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string Uid { get; set; }
        public string ImageUrl { get; set; }
        public int Followers { get; set; }
        public string Description { get; set; }
        public string StoreEmail { get; set; }
        public bool IsEmailPublic { get; set; }
        public string LegalName { get; set; }
        public string AffiliateId { get; set; }
        public string CurrencyUid { get; set; }
        public string StoreDescription { get; set; }
        public int LikesCount { get; set; }
        public string CurrencyCode { get; set; }
    }
}
