using System.Linq;
using System.Collections.Generic;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.MediaFiles;
using Core.Domain.Entities;

namespace Core.Application.Mappings
{
    public class PostProfile : AutoMapper.Profile
    {
        public PostProfile()
        {
            // TODO split to 2 endpoints
            CreateMap<GetPostsQuery, GetPostsQueryParams>();

            CreateMap<CreatePostCommand, CreatePostDto>()
                .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.Hashtags ?? new List<string>()));
            CreateMap<SharePostCommand, SharePostDto>();

            //CreateMap<AddMediaFileToPostCommand, PostMediaFileAddDto>();

            CreateMap<PostProductTag, PostProductTagResponse>()
                .ForMember(dest => dest.PositionLeftPercent, opt => opt.MapFrom(src => src.PositionLeftPercent))
                .ForMember(dest => dest.PositionTopPercent, opt => opt.MapFrom(src => src.PositionTopPercent))
                .ForMember(dest => dest.LocationX, opt => opt.MapFrom(src => src.LocationX))
                .ForMember(dest => dest.LocationY, opt => opt.MapFrom(src => src.LocationY))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.ThumbnailUrl))
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product));

            CreateMap<PostProfileMention, TaggedUserResponse>()
                .ForMember(dest => dest.ProfileUid, opt => opt.MapFrom(src => src.Profile.Uid))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Profile.User.UserName))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile.User.FirstName))
                .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.Profile.ImageUrl))
                .ForMember(dest => dest.FollowedByMe, opt => opt.Ignore()) // This will be set manually in queries
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.Profile.UserType));

            // Add Product mapping to ensure it's available for PostProductTag
            CreateMap<Product, ProductPublicResponse>()
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store != null ? src.Store.Name : null))
                .ForMember(dest => dest.ProductMediaFiles, opt => opt.MapFrom(src => src.ProductMediaFiles
                    .Where(pmf => pmf.MediaFile.IsActive)))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.ProductDetail, opt => opt.MapFrom(src => src.ProductDetail))
                .ForMember(dest => dest.ProductUrl, opt => opt.MapFrom(src => src.ProductUrl))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
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

            CreateMap<Post, PostDetailsResponse>()
                //.ForMember(dest => dest.PostStoreMentions, opt => opt.MapFrom(src => src.PostStoreMentions.Select(e => e.Store.UniqueName)))
                .ForMember(dest => dest.PostHashtags, opt => opt.MapFrom(src => src.PostHashtags.Select(h => h.Hashtag.Value)))
                .ForMember(dest => dest.PostProfileMentions, opt => opt.MapFrom(src => src.PostProfileMentions))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.ThumbnailUrl)
                        ? (src.MediaFile != null ? (src.MediaFile.OriginalUrl ?? src.MediaFile.Url) : null)
                        : src.ThumbnailUrl))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileDetailsResponse
                {
                    Uid = src.User.Profile.Uid,
                    //UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    //IsStore = false,
                    //IsInfluencer = src.User.Profile.IsInfluencer,
                    FullName  = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    FollowedByMe = false
                }));

            CreateMap<Post, PostResponse>()
                //.ForMember(dest => dest.PostStoreMentions, opt => opt.MapFrom(src => src.PostStoreMentions.Select(e => e.Store.UniqueName)))
                .ForMember(dest => dest.PostHashtags, opt => opt.MapFrom(src => src.PostHashtags.Select(h => h.Hashtag.Value)))
                .ForMember(dest => dest.PostProfileMentions, opt => opt.MapFrom(src => src.PostProfileMentions))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.ThumbnailUrl)
                        ? (src.MediaFile != null ? (src.MediaFile.OriginalUrl ?? src.MediaFile.Url) : null)
                        : src.ThumbnailUrl))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => new ProfileBaseResponse
                {
                    Uid = src.User.Profile.Uid,
                    UserId = src.User.Id,
                    ImageUrl = src.User.Profile.ImageUrl,
                    //IsStore = false,
                    //IsInfluencer = src.User.Profile.IsInfluencer,
                    FullName = src.User.FirstName,
                    FirstName = src.User.FirstName,
                    LastName = src.User.LastName,
                    Username = src.User.UserName,
                    DisplayName = src.User.DisplayName,
                    UserType = src.User.Profile.UserType,
                    FollowedByMe = false
                }));

            CreateMap<PagedList<Post>, PagingResponse<PostResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<PagedList<PostResponse>, PagingResponse<PostResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<PagedList<PostDetailsResponse>, PagingResponse<PostDetailsResponse>>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
