using Core.Application.Models;
using Core.Domain.Entities;
using Dashboard.Application.Mappings;
using Profile = AutoMapper.Profile;

namespace Dashboard.Application.Mediatr.Stores.Models;

public class StoreResponse : IMapFrom<Store>
{
    public string? Uid { get; set; }
    public string? Name { get; set; }
    public string? UniqueName { get; set; }
    public string? StoreEmail { get; set; }
    public string? ImageUrl { get; set; }
    public int ProductsCount { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Store, StoreResponse>()
            .ForMember(dest => dest.ProductsCount, opt => opt.MapFrom(src => src.Products.Count()));

        profile.CreateMap<PagedList<StoreResponse>, PagingResponse<StoreResponse>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src));
    }
}