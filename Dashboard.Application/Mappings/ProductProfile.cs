using Core.Application.Models;
using Core.Application.Models.Products;
using Core.Domain.Entities;

namespace Dashboard.Application.Mappings
{
    public class ProductProfile : AutoMapper.Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductAttribute, ProductAttributeResponse>();
            CreateMap<ProductMoreInfo, ProductMoreInfoResponse>();

            CreateMap<Product, ProductDetailsResponse>()
                //TODO change this response to return all product categories
                //.ForMember(dest => dest.CategoryUid, opt => opt.MapFrom(src => src.ProductCategory.Uid))
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Store.Currency.Code))
                //.ForMember(dest => dest.MoreInfos, opt => opt.MapFrom(src => src.ProductMoreInfos))
                //.ForMember(dest => dest.ProductPairArticleCodes, opt => opt.MapFrom(src => src.ProductPairs.Select(pp => pp.Pair.ArticleCode)))
                //.ForMember(dest => dest.ProductSimilarArticleCodes, opt => opt.MapFrom(src => src.ProductSimilars.Select(ps => ps.Similar.ArticleCode)))
                .ForMember(dest => dest.ProductMediaFiles, opt => opt.MapFrom(src => src.ProductMediaFiles.Select( pmf => pmf.MediaFile)));

            CreateMap<PagedList<ProductInventoryResponse>, PagingResponse<ProductInventoryResponse>>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
