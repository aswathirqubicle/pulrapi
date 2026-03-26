using System.Collections.Generic;
using Core.Application.Mappings;
using Core.Application.Models.Stories;
using Core.Domain.Entities;

namespace Core.Application.Models.Stores
{
    public class StoreDetailsResponse : IMapFrom<Store>
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string ImageUrl { get; set; }
        public string BannerUrl { get; set; }
        public int Followers { get; set; }
        public bool FollowedByMe { get; set; }
        public List<string> FollowedBy { get; set; } = new List<string>();
        public string Description { get; set; }
        public string StoreEmail { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsEmailPublic { get; set; }
        public int ProductsCount { get; set; }
        public string LegalName { get; set; }
        public string Location { get; set; }
        public string WebsiteUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string TikTokUrl { get; set; }
        public string AffiliateId { get; set; }
        public string CurrencyUid { get; set; }
        public string StoreDescription { get; set; }
        public string CurrencyCode { get; set; }
        public double RatingAverage { get; set; }
        public bool IsStore { get; set; }
        public bool IsMyStore { get; set; }
        public int LikesCount { get; set; }
        public int PostsCount { get; set; }
        public string UserId { get; internal set; }
        public string ProfileUid { get; internal set; }
        public List<StoryResponse> Stories { get; set; } = new List<StoryResponse>();
        public int ActiveStoriesCount { get; internal set; }
        public int StoriesSeenCount { get; internal set; }
        public int UnseenStoriesCount { get; internal set; }
    }
}