using System;
using System.Collections.Generic;
using System.Linq;
using Core.Application.Mappings;
using Core.Application.Models.Currencies;
using Core.Application.Models.Stores;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Models.Profiles;

public class ProfileDetailsResponse : IMapFrom<Profile>
{
    public string Uid { get; set; }
    public string ImageUrl { get; set; }
    public string FirstName { get; set; }
    public string FullName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string About { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsProfilePublic { get; set; } // Assuming this is a string representation of a boolean
    public string Gender { get; set; }
    public string Location { get; set; }
    public string UserType { get; set; }
    public string WebsiteUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public ICollection<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; } = new List<ProfileSocialMediaLinkDto>();
    public string Email { get; set; }
    public bool FollowedByMe { get; set; }
    public bool FollowRequestSent { get; set; } // True when current user has sent any follow request (regular or followback)
    public bool FollowRequestReceived { get; set; }
    public bool CanFollowBack { get; set; } // True when the viewed profile follows current user but current user doesn't follow back
    public int Followers { get; set; }
    public int Following { get; set; }
    public int AllPostsLikesCount { get; set; }
    public int ReportsCount { get; set; }
    public List<string> FollowedBy { get; set; } = new List<string>();
    public List<string> MutualFollowers { get; set; } = new List<string>();
    public List<StoreDetailsResponse> Stores { get; set; } = new List<StoreDetailsResponse>();
    public List<string> StoreUids { get; set; } = new List<string>();

    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string CityName { get; set; }
    public string CountryName { get; set; }
    public string CountryUid { get; set; }
    public bool IsProfileBio { get; set; }
    public CurrencyDetailsResponse Currency { get; set; }

    public int PostsCount { get; set; }
    public int ActiveStoriesCount { get; set; }
    public int StoriesSeenCount { get; set; }
    public int UnseenStoriesCount { get; set; }
    public List<StoryResponse> Stories { get; set; } = new List<StoryResponse>();
    public bool IsInfluencer { get; set; }
    public DateTime PostedTimeAgo { get; set; }
    public bool IsStore { get; internal set; }
    public string StoreName { get; internal set; }
    public string StoreUniqueName { get; internal set; }
    public DateTime CreatedAt { get; internal set; }

    public void Map(AutoMapper.Profile profile)
    {
        profile.CreateMap<Profile, ProfileDetailsResponse>()
            .ForMember(dest => dest.Uid, opt => opt.MapFrom(src => src.Uid))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Name : src.Location))
            .ForMember(dest => dest.CountryUid, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Uid : null))
            .ForMember(dest => dest.Followers, opt => opt.MapFrom(src => src.ProfileFollowers.Count))
            .ForMember(dest => dest.Following, opt => opt.MapFrom(src => src.ProfileFollowings.Count))
            .ForMember(dest => dest.PostsCount, opt => opt.MapFrom(src => src.User.Posts.Count))
            // ActiveStoriesCount is calculated manually in queries with current user context
            .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.UserType))
            .ForMember(dest => dest.Stores, opt => opt.MapFrom(src => src.User.Stores))
            .ForMember(dest => dest.ReportsCount, opt => opt.MapFrom(src => src.Reports.Count(r => r.ReportType == ReportTypeEnum.Profile)));

        profile.CreateMap<User, ProfileDetailsResponse>();
        profile.CreateMap<Store, StoreDetailsResponse>()
            .ForMember(dest => dest.Followers, opt => opt.MapFrom(src => src.StoreFollowers.Count))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.Uid, opt => opt.MapFrom(src => src.Uid))
            .ForMember(dest => dest.UniqueName, opt => opt.MapFrom(src => src.UniqueName));
    }
}