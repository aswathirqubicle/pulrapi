using System.Collections.Generic;
using Core.Application.Models.Currencies;
using Core.Application.Models.Stores;

namespace Core.Application.Models.Profiles
{
    public class MyProfileDetailsResponse
    {
        public string Uid { get; set; }
        public string ImageUrl { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string About { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }

        public string UserType { get; set; }
        public bool? IsProfilePublic { get; set; }

        public string Location { get; set; }
        public string WebsiteUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string TikTokUrl { get; set; }
        public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; } = new List<ProfileSocialMediaLinkDto>();
        public string Email { get; set; }

        public string CurrencyUid { get; set; }

        // Counts
        public int Followers { get; set; }
        public int Following { get; set; }
        public int PostsCount { get; set; }
        public int AllPostsLikesCount { get; set; }

        // this will be profile uids, cause username can be changed and we loose track of followers:
        // public List<string> Followers { get; set; }
        // public List<string> Following { get; set; }
        public List<StoreDetailsResponse> Stores { get; set; }
        public List<string> StoreUids { get; set; }
        public CurrencyDetailsResponse Currency { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string CityName { get; set; }
        public string CountryUid { get; set; }
    }
}