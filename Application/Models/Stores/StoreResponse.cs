using Core.Application.Models.Stores;

namespace Core.Application.Models.Stores
{
    public class StoreResponse : StoreBaseResponse
    {
        public string BannerUrl { get; set; }
        public string Description { get; set; }
        public string StoreEmail { get; set; }
        public bool IsEmailPublic { get; set; }
        public int Followers { get; set; }
        public double RatingAverage { get; set; }
        public int ProductsCount { get; set; }
        public int LikesCount { get; set; }
    }
}
