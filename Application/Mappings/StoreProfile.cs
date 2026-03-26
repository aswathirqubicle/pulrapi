using System.Linq;
using AutoMapperProfile = AutoMapper.Profile;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.Products;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Application.Models.Profiles;
using Core.Application.Models.MediaFiles;

namespace Core.Application.Mappings
{
    public class StoreProfile : AutoMapperProfile
    {
        public StoreProfile()
        {
            CreateMap<Currency, CurrencyDetailsResponse>();

            CreateMap<CreateStoreCommand, StoreCreateDto>();
            CreateMap<UpdateStoreCommand,StoreUpdateDto>();

            CreateMap<PagedList<StoreResponse>, PagingResponse<StoreResponse>>().ForMember(
                           dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<GetPublicProductsQuery, ProductPublicListRequestParams>(); 
            CreateMap<ProductCreateCommand, ProductCreateDto>();
            CreateMap<ProductUpdateCommand, ProductUpdateDto>();
            CreateMap<UpdateProductImagesCommand, ProductImagesUpdateDto>().ForMember(
                dest => dest.ImagePriorities, opt => opt.MapFrom(src => src.ImagePriorities.Select(p => int.Parse(p))));
            CreateMap<DeleteProductImageCommand, ProductImageDeleteDto>();
            CreateMap<Product, ProductDetailsResponse>()
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.ProductDetail, opt => opt.MapFrom(src => src.ProductDetail))
                .ForMember(dest => dest.ProductUrl, opt => opt.MapFrom(src => src.ProductUrl))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.SellType, opt => opt.MapFrom(src => src.SellType))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileBaseResponse
                {
                    Uid = src.User.Profile.Uid,
                    UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    //IsStore = false,
                    FullName = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    UserType = src.User.Profile.UserType,
                    FollowedByMe = false
                }));
            CreateMap<Product, ProductInventoryResponse>()
                //TODO fix this mapping for category listing 
                /*.ForMember(
                dest => dest.CategoryTitle, opt => opt.MapFrom(src => src.ProductCategory.Category.Name))*/
                .ForMember(
                dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ProductMediaFiles.Where(pmf => pmf.MediaFile.Priority == 0).FirstOrDefault().MediaFile.Url));
            CreateMap<PagedList<Product>, PagingResponse<ProductInventoryResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<Product, ProductPublicResponse>()
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store != null ? src.Store.Name : null))
                .ForMember(dest => dest.ProductMediaFiles, opt => opt.MapFrom(src => src.ProductMediaFiles
                    .Where(pmf => pmf.MediaFile.IsActive)))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.ProductDetail, opt => opt.MapFrom(src => src.ProductDetail))
                .ForMember(dest => dest.ProductUrl, opt => opt.MapFrom(src => src.ProductUrl))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.SellType, opt => opt.MapFrom(src => src.SellType))
                .ForMember(dest => dest.CountryCode, opt => opt.MapFrom(src => src.Country != null ? src.Country.Iso2 : null))
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Country != null ? src.Country.Iso4 : null))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileBaseResponse
                {
                    Uid = src.User.Profile.Uid,
                    UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    //IsStore = false,
                    FullName = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    UserType = src.User.Profile.UserType,
                    FollowedByMe = false
                }));

            CreateMap<PagedList<Product>, PagingResponse<ProductPublicResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            // Add mapping for PagedList<ProductDetailsResponse> to PagingResponse<ProductDetailsResponse>
            CreateMap<PagedList<ProductDetailsResponse>, PagingResponse<ProductDetailsResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
