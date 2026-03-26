using AutoMapper;
using Core.Application.Mappings;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Profiles;

public class ProfileBioDto : IMapFrom<Domain.Entities.Profile>
{
    public string Uid { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string UserType { get; set; }
    public string About { get; set; }
    public string Location { get; set; }
    public string PhoneNumber { get; set; }
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; } = new List<ProfileSocialMediaLinkDto>();
    public string Message { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Domain.Entities.Profile, ProfileBioDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
            // .ForMember(dest => dest.About, opt => opt.MapFrom(src => src.About))
            // .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.WebsiteUrl))
            .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.FacebookUrl))
            .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.InstagramUrl))
            .ForMember(dest => dest.TwitterUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TwitterUrl))
            .ForMember(dest => dest.TikTokUrl, opt => opt.MapFrom(src => src.ProfileSocialMedia.TikTokUrl))
            .ForMember(dest => dest.SocialMediaLinks, opt => opt.MapFrom(src => src.ProfileSocialMediaLinks));
    }
}