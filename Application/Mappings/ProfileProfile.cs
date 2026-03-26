using System.Linq;
using AutoMapperProfile = AutoMapper.Profile;
using Core.Application.Mediatr.Profiles.Commands;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Core.Domain.Views;
using Core.Application.Models.Currencies;
using Core.Application.Models.Stores;

namespace Core.Application.Mappings
{
    public class ProfileProfile : AutoMapperProfile
    {
        public ProfileProfile()
        {
            CreateMap<Profile, ProfileDetailsResponse>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.Key))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Name : src.Location))
                .ForMember(dest => dest.CountryUid, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Uid : null))
                .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.WebsiteUrl))
                .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.InstagramUrl))
                .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.FacebookUrl))
                .ForMember(dest => dest.TwitterUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TwitterUrl))
                .ForMember(dest => dest.TikTokUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TikTokUrl))
                .ForMember(dest => dest.SocialMediaLinks, opt => opt.MapFrom(src => src.ProfileSocialMediaLinks));

            CreateMap<ProfileSocialMediaLink, ProfileSocialMediaLinkDto>();

            CreateMap<Profile, MyProfileDetailsResponse>()
                .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.FacebookUrl))
                .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.InstagramUrl))
                .ForMember(dest => dest.TwitterUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TwitterUrl))
                .ForMember(dest => dest.TikTokUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TikTokUrl))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Name : src.Location))
                .ForMember(dest => dest.CountryUid, opt => opt.MapFrom(src => src.User.Country != null ? src.User.Country.Uid : null))
                .ForMember(dest => dest.SocialMediaLinks, opt => opt.MapFrom(src => src.ProfileSocialMediaLinks));

            CreateMap<UpdateMyProfileCommand, MyProfileDetailsResponse>()
                .ForMember(dest => dest.Uid, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            CreateMap<UpdateMyProfileCommand, ProfileUpdateDto>();

            CreateMap<Profile, ProfileDetailsResponse>()
                .ForMember(dest => dest.Followers, opt => opt.MapFrom(src => src.ProfileFollowers.Count()))
                .ForMember(dest => dest.Following, opt => opt.MapFrom(src => src.ProfileFollowings.Count()));

            CreateMap<ProfileFollowingView, ProfileDetailsResponse>()
                .ForMember(dest => dest.Stores, opt => opt.MapFrom(src => src.Stores));

            CreateMap<PagedList<Profile>, PagingResponse<ProfileDetailsResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<PagedList<ProfileDetailsResponse>, PagingResponse<ProfileDetailsResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<PagedList<ProfileFollowingView>, PagingResponse<ProfileDetailsResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<Profile, ProfileBioDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.WebsiteUrl))
                .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.FacebookUrl))
                .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.InstagramUrl))
                .ForMember(dest => dest.TwitterUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TwitterUrl))
                .ForMember(dest => dest.TikTokUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TikTokUrl))
                .ForMember(dest => dest.SocialMediaLinks, opt => opt.MapFrom(src => src.ProfileSocialMediaLinks));
        }
    }
}
