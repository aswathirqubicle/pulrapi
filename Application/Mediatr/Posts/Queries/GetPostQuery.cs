using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Posts.Queries
{
    public class GetPostQuery : IRequest<PostDetailsResponse>
    {
        [Required]
        public string Uid { get; set; }
        public string CurrencyCode { get; set; }
        public string Username { get; set; } // Optional: fetch by username if provided
        [EnumDataType(typeof(ProductTypeEnum))]
        public ProductTypeEnum? ProductType { get; set; } // Optional: filter by product type
    }

    public class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDetailsResponse>
    {
        private readonly ILogger<GetPostQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IExchangeRateService _exchangeRateService;

        public GetPostQueryHandler(ILogger<GetPostQueryHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IExchangeRateService exchangeRateService)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<PostDetailsResponse> Handle(GetPostQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var uid = request.Uid;
                var currencyCode = request.CurrencyCode != null ? await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == request.CurrencyCode, cancellationToken) : null;

                var cUser = await _currentUserService.GetUserAsync();

                // Try to find by post UID first
                var queryPost = _dbContext.Posts.Where(p => p.Uid == uid && !p.User.IsSuspended && p.IsActive);
                var fetchedPost = await queryPost
                    .Include(p => p.User).ThenInclude(u => u.Profile)
                    .Include(p => p.PostProfileMentions)
                        .ThenInclude(ppm => ppm.Profile)
                            .ThenInclude(pr => pr.User)
                    .Include(p => p.PostProductTags)
                    .ThenInclude(ppt => ppt.Product)
                    .ThenInclude(ppp => ppp.Store).ThenInclude(s => s.Currency)
                    .Include(p => p.PostClicks).SingleOrDefaultAsync();

                // If not found, try by username
                if (fetchedPost == null)
                {
                    queryPost = _dbContext.Posts.Where(p => p.User.UserName == uid && !p.User.IsSuspended && p.IsActive);
                    fetchedPost = await queryPost
                        .Include(p => p.User).ThenInclude(u => u.Profile)
                        .Include(p => p.PostProfileMentions)
                            .ThenInclude(ppm => ppm.Profile)
                                .ThenInclude(pr => pr.User)
                        .Include(p => p.PostProductTags)
                        .ThenInclude(ppt => ppt.Product)
                        .ThenInclude(ppp => ppp.Store).ThenInclude(s => s.Currency)
                        .Include(p => p.PostClicks).SingleOrDefaultAsync();
                }

                if (fetchedPost == null)
                {
                    throw new BadRequestException($"Post with uid or username {uid} not found.");
                }

                // Blocking enforcement: check if current user is blocked by post owner or vice versa
                if (cUser?.Profile != null)
                {
                    var isBlocked = await _dbContext.UserBlocks.AnyAsync(
                        ub => ub.IsActive && (
                            (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == fetchedPost.User.Profile.Uid) ||
                            (ub.BlockerProfileId == fetchedPost.User.Profile.Uid && ub.BlockedProfileId == cUser.Profile.Uid)
                        ),
                        cancellationToken);

                    if (isBlocked)
                    {
                        throw new ForbiddenException("You cannot view this content.");
                    }
                }

                // Privacy enforcement: if post owner's profile is private, allow only owner or followers
                var postOwnerPrivacy = await _dbContext.Profiles
                    .Where(pr => pr.Id == fetchedPost.User.Profile.Id)
                    .Select(pr => new
                    {
                        ProfileId = pr.Id,
                        ProfileUid = pr.Uid,
                        IsProfilePublic = pr.ProfileSettings == null || pr.ProfileSettings.IsProfilePublic
                    })
                    .SingleAsync(cancellationToken);

                if (!postOwnerPrivacy.IsProfilePublic)
                {
                    var isOwner = cUser?.Profile?.Uid == postOwnerPrivacy.ProfileUid;
                    var isFollower = false;
                    if (!isOwner && cUser?.Profile != null)
                    {
                        isFollower = await _dbContext.ProfileFollowers.AnyAsync(
                            pf => pf.ProfileId == postOwnerPrivacy.ProfileId && pf.FollowerId == cUser.Profile.Id,
                            cancellationToken);
                    }

                    if (!isOwner && !isFollower)
                    {
                        throw new ForbiddenException("This profile is private.");
                    }
                }

                // Filter by product type if provided
                if (request.ProductType.HasValue)
                {
                    // Check if the post has any products of the specified type
                    var hasMatchingProductType = fetchedPost.PostProductTags.Any(ppt => ppt.Product.Type == request.ProductType.Value);
                    if (!hasMatchingProductType)
                    {
                        throw new BadRequestException($"Post with uid {uid} does not contain products of the specified type.");
                    }
                }

                var existingPostClick = await _dbContext.PostClicks.SingleOrDefaultAsync(pc => pc.Post.Id == fetchedPost.Id && pc.User == cUser);
                if (existingPostClick != null)
                {
                    existingPostClick.Count += 1;
                }
                else
                {
                    fetchedPost.PostClicks.Add(new PostClick() { Post = fetchedPost, User = cUser, Count = 1 });
                }
                await _dbContext.SaveChangesAsync(CancellationToken.None);


                List<string> currencyCodes = null;
                List<string> storeCurrencyCodes = null;
                List<ExchangeRate> exchangeRates = null;
                bool doExchangeRate = false;
                Currency currency = null;

                if (currencyCode != null && fetchedPost.PostProductTags.Any())
                {
                    storeCurrencyCodes = fetchedPost.PostProductTags.DistinctBy(ppt => ppt.Product.Store.Currency.Code).Select(ppt => ppt.Product.Store.Currency.Code).ToList();
                    currencyCodes = new List<string>() { currencyCode.Code };
                    currencyCodes.AddRange(storeCurrencyCodes);
                    exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);
                    currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == currencyCode.Code, cancellationToken);
                    doExchangeRate = currencyCodes != null && storeCurrencyCodes.Any() && exchangeRates != null;
                }
                else
                {
                    //currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]);

                }

                var postRes = await queryPost.Select(post => new PostDetailsResponse()
                {
                    Uid = post.Uid,
                    Text = post.Text,
                    ImgDescription = post.ImgDescription,
                    ThumbnailUrl = string.IsNullOrEmpty(post.ThumbnailUrl) ? (post.MediaFile != null ? (post.MediaFile.OriginalUrl ?? post.MediaFile.Url) : null) : post.ThumbnailUrl,
                    ImageWidth = post.ImageWidth ?? 500,
                    ImageHeight = post.ImageHeight ?? 500,
                    VideoWidth = post.VideoWidth,
                    VideoHeight = post.VideoHeight,
                    CreatedAt = post.CreatedAt,
                    LikesCount = post.PostLikes.Count(),
                    CommentsCount = post.Comments.Count(c => c.IsActive),
                    BookmarksCount = post.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
                    BookmarkedByMe = cUser != null && post.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == cUser.Profile.Id && bci.BookmarkCollection.IsActive),
                    MyStylesCount = post.PostMyStyles.Count,
                    Location = post.Location,
                    LikedByMe = cUser != null ? post.PostLikes.Any(pl => pl.LikedById == cUser.Profile.Id) : false,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(post.MediaFile),
                    PostHashtags = post.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                    PostProfileMentions = post.PostProfileMentions.Select(e => new TaggedUserResponse
                    {
                        ProfileUid = e.Profile.Uid,
                        Username = e.Profile.User.UserName,
                        FirstName = e.Profile.User.FirstName,
                        ProfileImageUrl = e.Profile.ImageUrl,
                        FollowedByMe = cUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id),
                        UserType = e.Profile.UserType
                    }).ToList(),
                    PostStoreMentions = post.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                    PostType = PostTypeEnum.Feed,
                    ProfileUid = post.Store == null ? post.User.Profile.Uid : null,
                    Profile = new ProfileDetailsResponse()
                    {
                        Uid = post.User.Profile.Uid,
                        Username = post.User.UserName,
                        FullName = post.User.FirstName,
                        FirstName = post.User.FirstName,
                        DisplayName = post.User.DisplayName,
                        LastName = post.User.LastName,
                        ImageUrl = post.User.Profile.ImageUrl,
                        Location = post.User.Profile.Location,
                        FollowedByMe = cUser != null ? post.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id) : false,
                    },
                    //Store = new StoreDetailsResponse()
                    //{
                    //    Uid = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().Uid,
                    //    Name = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().Name,
                    //    UniqueName = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().UniqueName,
                    //    ImageUrl = post.PostProductTags.Select(t => t.Product.Store).FirstOrDefault().ImageUrl,
                    //    FollowedByMe = cUser != null ? post.Store.StoreFollowers.Any(sf => sf.FollowerId == cUser.Profile.Id) : false,
                    //},
                    StoreUid = post.Store != null ? post.Store.Uid : null,
                    PostedByStore = post.Store != null,
                    PostProductTags = post.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse()
                    {
                        Product = new ProductPublicResponse()
                        {
                            Uid = e.Product.Uid,
                            Name = e.Product.Name,
                            WhatIsIt = e.Product.WhatIsIt,
                            ProductDetail = e.Product.ProductDetail,
                            Brand = e.Product.Brand,
                            MinPrice = e.Product.MinPrice,
                            MaxPrice = e.Product.MaxPrice,
                            ProductUrl = e.Product.ProductUrl,
                            Type = e.Product.Type,
                            Price = doExchangeRate ? _exchangeRateService.GetCurrencyExchangeRates(e.Product.Store.Currency.Code, currencyCode.Code, e.Product.MinPrice, exchangeRates) : e.Product.MinPrice,
                            CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
                            CurrencyCode = currency != null ? currency.Code : (e.Product.Country != null ? e.Product.Country.Iso4 : null),
                            StoreName = e.Product.Store.Name,
                            ProductMediaFiles = e.Product.ProductMediaFiles
                                .Where(pmf => pmf.MediaFile.IsActive)
                                .Select(pmf => new MediaFileDetailsResponse
                                {
                                    Uid = pmf.MediaFile.Uid,
                                    Url = pmf.MediaFile.Url,
                                    FileType = pmf.MediaFile.MediaFileType.ToString(),
                                    Priority = pmf.MediaFile.Priority,
                                    IsHlsProcessed = pmf.MediaFile.IsHlsProcessed,
                                    OriginalUrl = pmf.MediaFile.OriginalUrl,
                                    HlsBasePath = pmf.MediaFile.HlsBasePath,
                                    VideoDurationSeconds = pmf.MediaFile.VideoDurationSeconds,
                                    AvailableQualities = pmf.MediaFile.AvailableQualities,
                                    IsMuted = pmf.MediaFile.IsMuted,
                                    CropX = pmf.MediaFile.CropX,
                                    CropY = pmf.MediaFile.CropY,
                                    CropWidth = pmf.MediaFile.CropWidth,
                                    CropHeight = pmf.MediaFile.CropHeight
                                }).ToList(),
                            ProductVariants = e.Product.ProductVariant
                            .Select(pv => new ProductVariantResponse
                            {
                                VariantName = pv.VariantName,
                                VariantOptions = pv.ProductVariantOptions.Select(opt => opt.Value).ToList(),
                            }).ToList(),
                            Profile = new ProfileBaseResponse
                            {
                                Uid = e.Product.User.Profile.Uid,
                                Username = e.Product.User.Profile.User.UserName,
                                FullName = e.Product.User.Profile.User.FirstName,
                                FirstName = e.Product.User.Profile.User.FirstName,
                                LastName = e.Product.User.Profile.User.LastName,
                                DisplayName = e.Product.User.Profile.User.DisplayName,
                                ImageUrl = e.Product.User.Profile.ImageUrl,
                                FollowedByMe = cUser != null && e.Product.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id)
                            }
                        },
                        PositionLeftPercent = e.PositionLeftPercent,
                        PositionTopPercent = e.PositionTopPercent,
                        LocationX = e.LocationX,
                        LocationY = e.LocationY,                      
                    }).ToList()
                }).SingleOrDefaultAsync(cancellationToken);


                if (cUser?.Profile != null)
                {
                    postRes.IsMyStyle = await _dbContext.PostMyStyles.AnyAsync(pms => pms.Post.Uid == postRes.Uid && pms.Profile.Id == cUser.Profile.Id);
                }

                if (postRes.PostProductTags.Any())
                {
                    postRes.TaggedProductUids = fetchedPost.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => ppt.Product.Uid).ToList();
                }

                return postRes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
