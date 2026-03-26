using Core.Application.Mappings;
using Core.Domain.Entities;
using Profile = AutoMapper.Profile;

namespace Core.Application.Models.Stores;

public class StoreBioDto : IMapFrom<Store>
{
    public string Uid { get; set; }
    public string UniqueName { get; set; }
    public string About { get; set; }
    public string Location { get; set; }
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Store, StoreBioDto>()
            .ForMember(dest => dest.UniqueName, opt => opt.MapFrom(src => src.UniqueName))
            .ForMember(dest => dest.WebsiteUrl, opt => opt.MapFrom(src => src.StoreSocialMedia.WebsiteUrl))
            .ForMember(dest => dest.FacebookUrl, opt => opt.MapFrom(src => src.StoreSocialMedia.FacebookUrl))
            .ForMember(dest => dest.InstagramUrl, opt => opt.MapFrom(src => src.StoreSocialMedia.InstagramUrl))
            .ForMember(dest => dest.TwitterUrl, opt => opt.MapFrom(src => src.StoreSocialMedia.TwitterUrl))
            .ForMember(dest => dest.TikTokUrl, opt => opt.MapFrom(src => src.StoreSocialMedia.TikTokUrl));
    }
}