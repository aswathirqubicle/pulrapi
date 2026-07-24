using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Posts.Queries;

public class GetUserFollowingFeedQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
    //public string CurrencyCode { get; set; }
    public ProductTypeEnum? ProductType { get; set; }
}

public class GetUserFollowingFeedQueryHandler : IRequestHandler<GetUserFollowingFeedQuery, PagingResponse<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    public GetUserFollowingFeedQueryHandler(
        IApplicationDbContext dbContext, 
        UserManager<User> userManager,
        IExchangeRateService exchangeRateService,
        ICurrentUserService currentUserService,
        ILogger<GetUserFollowingFeedQueryHandler> logger, IMapper mapper)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _exchangeRateService = exchangeRateService;
        _currentUserService = currentUserService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<PagingResponse<PostResponse>> Handle(GetUserFollowingFeedQuery request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ✅ Log start of operation
            _logger.LogInformation(
                "GetUserFollowingFeed started for PageNumber={PageNumber}, PageSize={PageSize}, Search={Search}",
                request.PageNumber,
                request.PageSize,
                request.Search ?? "none");

            var currentUser = await _currentUserService.GetUserAsync();

            //Load ONLY the fields we need (Id and Uid)
            var currentProfile = await _dbContext.Profiles
                .Where(p => p.IsActive && p.UserId == currentUser.Id)
                .Select(p => new { p.Id, p.Uid })
                .SingleOrDefaultAsync(cancellationToken);

            if(currentProfile == null)
            {
                throw new Exception("Current user profile not found");
            }

            var currentProfileId = currentProfile.Id;
            var currentProfileUid = currentProfile.Uid;

            // ✅ OPTIMIZED: Get all influencer user IDs in ONE query (instead of per-post)
            var influencerRoleId = await _dbContext.Roles
                .Where(r => r.Name == PulrRoles.Influencer)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var influencerUserIds = influencerRoleId != null
                ? await _dbContext.UserRoles
                    .Where(ur => ur.RoleId == influencerRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync(cancellationToken)
                : new List<string>();


            // Filter posts directly in database using a subquery
            var postsQuery = _dbContext.Posts
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.User.Id != currentUser.Id
                            && !p.User.IsSuspended
                            // Feed sources: Following, Store followers, Product tags, Store mentions
                            && (
                            // Posts from followed profiles
                            _dbContext.ProfileFollowers
                                .Where(pf => pf.FollowerId == currentProfileId && !pf.Profile.User.IsSuspended)
                                .Select(pf => pf.ProfileId)
                                .Contains(p.User.Profile.Id)

                            // Posts with products from followed stores
                            || p.PostProductTags.Any(ppt =>
                                ppt.Product.Store.StoreFollowers.Any(sf => sf.FollowerId == currentProfileId))

                            // Posts mentioning followed stores
                            || p.PostStoreMentions.Any(psm =>
                                psm.Store.StoreFollowers.Any(sf => sf.FollowerId == currentProfileId))                   
                            )

                             // Filter blocked users (bidirectional: both blocking and being blocked)
                             && !_dbContext.UserBlocks
                                .Where(ub => ub.IsActive && (
                                    (ub.BlockerProfileId == currentProfileUid && ub.BlockedProfileId == p.User.Profile.Uid) ||
                                    (ub.BlockerProfileId == p.User.Profile.Uid && ub.BlockedProfileId == currentProfileUid)
                                ))
                                .Any()

                            //Subquery: Check if post is reported
                            && !_dbContext.Reports
                                .Where(r => r.ReportType == ReportTypeEnum.Post)
                                .Select(r => r.EntityUid)
                                .Contains(p.Uid)
                           
                            //Check if profile is public OR I'm following them
                            && (p.User.Profile.ProfileSettings == null // No settings = public
                                || p.User.Profile.ProfileSettings.IsProfilePublic // Explicitly public
                                || _dbContext.ProfileFollowers // OR I'm following them
                                    .Any(pf => pf.FollowerId == currentProfileId
                                            && pf.ProfileId == p.User.Profile.Id))
                 );

            // Filter by product type if provided
            if (request.ProductType.HasValue)
            {
                postsQuery = postsQuery.Where(p => 
                    p.PostProductTags.Any(ppt => ppt.Product.Type == request.ProductType.Value));
            }

            if (!String.IsNullOrWhiteSpace(request.Search))
            {
                if (request.Search.StartsWith("#"))
                {
                    var searchWithoutHashtag = request.Search.Replace("#", "");
                    postsQuery = postsQuery.Where(p =>
                        p.PostHashtags.Any(ph => EF.Functions.Like(ph.Hashtag.Value, $"%{searchWithoutHashtag}%")));
                }
                else
                {
                    postsQuery = postsQuery.Where(p =>
                        EF.Functions.Like(p.User.UserName, $"%{request.Search}%") ||
                        EF.Functions.Like(p.Store.UniqueName, $"%{request.Search}%"));
                }
            }
            
            // List<string> currencyCodes = null;
            // List<ExchangeRate> exchangeRates = null;
            // if (request.CurrencyCode != null)
            //     exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);

            var queryMapped = postsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponse()
                {
                    Uid = p.Uid,
                    ProfileUid = p.User.Profile.Uid,
                    Text = p.Text,
                    ImageWidth = p.ImageWidth ?? 500,
                    ImageHeight = p.ImageHeight ?? 500,
                    VideoWidth = p.VideoWidth,
                    VideoHeight = p.VideoHeight,
                    ThumbnailUrl = string.IsNullOrEmpty(p.ThumbnailUrl) ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null) : p.ThumbnailUrl,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(p.MediaFile),
                    LikesCount = p.PostLikes.Count(),
                    LikedByMe = p.PostLikes.Any(pl => pl.LikedById == currentProfileId),
                    TaggedProductUids = p.PostProductTags
                        .Where(ppt => ppt.Product != null && ppt.Product.IsActive)
                        .Select(ppt => ppt.Product.Uid),
                    CreatedAt = p.CreatedAt,
                    //PostedByStore = p.Store != null,

                    // Calculate share count in databse
                    ShareCount = _dbContext.Posts.Count(sp => sp.IsActive && sp.SharedPostId == p.Id),

                    // Calculate SharedByMe in database
                    SharedByMe = _dbContext.Posts.Any(sp =>
                        sp.IsActive &&
                        sp.SharedPostId == p.Id &&
                        sp.User.Profile.Id == currentProfileId),

                    //Store = p.Store == null
                    //    ? null
                    //    : new StoreBaseResponse()
                    //    {
                    //        Uid = p.Store.Uid,
                    //        Name = p.Store.Name,
                    //        ImageUrl = p.Store.ImageUrl,
                    //        UniqueName = p.Store.UniqueName,
                    //        //CurrencyCode = p.Store.Currency.Code,
                    //        FollowedByMe = currentUser != null &&
                    //                       p.Store.StoreFollowers.Any(sf => sf.FollowerId == currentUser.Profile.Id),
                    //    },
                    Profile = p.Store == null
                        ? new ProfileBaseResponse()
                        {
                            Uid = p.Uid,
                            UserId = p.User.Profile.Uid,
                            FullName = p.User.FirstName,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            //IsStore = p.Store != null,
                            ImageUrl = p.User.Profile.ImageUrl,
                            Username = p.User.UserName,
                            FollowedByMe = p.User.Profile.ProfileFollowers.Any(e =>
                                               e.FollowerId == currentProfileId),
                        }
                        : null,
                    PostProductTags = p.PostProductTags
                        .Where(pt => pt.Product != null && pt.Product.IsActive)
                        .Select(pt => new PostProductTagResponse()
                        {
                            Product = new ProductPublicResponse()
                            {
                                Uid = pt.Product.Uid,
                                Name = pt.Product.Name,
                                WhatIsIt = pt.Product.WhatIsIt,
                                ProductDetail = pt.Product.ProductDetail,
                                Brand = pt.Product.Brand,
                                MinPrice = pt.Product.MinPrice,
                                MaxPrice = pt.Product.MaxPrice,
                                ProductUrl = pt.Product.ProductUrl,
                                StoreName = pt.Product.Store.Name,
                                CountryCode = pt.Product.Country != null ? pt.Product.Country.Iso2 : null,
                                CurrencyCode = pt.Product.Country != null ? pt.Product.Country.Iso4 : null,
                                ProductMediaFiles = pt.Product.ProductMediaFiles
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
                                        AvailableQualities = pmf.MediaFile.AvailableQualities
                                    }).ToList(),
                                ProductVariants = pt.Product.ProductVariant
                                    .Select(pv => new ProductVariantResponse
                                    {
                                        VariantName = pv.VariantName,
                                        VariantOptions = pv.ProductVariantOptions.Select(opt => opt.Value).ToList()
                                    }).ToList(),
                                Profile = new ProfileBaseResponse
                                {
                                    Uid = pt.Product.User.Profile.Uid,
                                    UserId = pt.Product.User.Id,
                                    ImageUrl = pt.Product.User.Profile.ImageUrl,
                                    //IsStore = false,
                                    FullName = pt.Product.User.FirstName,
                                    FirstName = pt.Product.User.FirstName,
                                    LastName = pt.Product.User.LastName,
                                    Username = pt.Product.User.UserName,
                                    DisplayName = pt.Product.User.DisplayName,
                                    UserType = pt.Product.User.Profile.UserType,
                                    FollowedByMe = currentUser != null && pt.Product.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentUser.Profile.Id)
                                }
                            },
                            PositionLeftPercent = pt.PositionLeftPercent,
                            PositionTopPercent = pt.PositionTopPercent,
                            LocationX = pt.LocationX,
                            LocationY = pt.LocationY
                        }).ToList(),

                    PostProfileMentions = p.PostProfileMentions.Select(e => new TaggedUserResponse
                    {
                        ProfileUid = e.Profile.Uid,
                        Username = e.Profile.User.UserName,
                        FirstName = e.Profile.User.FirstName,
                        ProfileImageUrl = e.Profile.ImageUrl,
                        FollowedByMe = currentUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentUser.Profile.Id),
                        UserType = e.Profile.UserType
                    }).ToList(),

                    PostHashtags = p.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                    CommentsCount = p.Comments.Count,
                    BookmarkedByMe = currentUser != null && p.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci.BookmarkCollection.IsActive),
                    IsMyStyle = currentUser != null && p.PostMyStyles.Any(b => b.ProfileId == currentUser.Profile.Id),
                    BookmarksCount = p.BookmarkCollectionItems
                        .Where(bci => bci.BookmarkCollection.IsActive)
                        .Select(bci => bci.BookmarkCollection.ProfileId)
                        .Distinct()
                        .Count(),
                    MyStylesCount = p.PostMyStyles.Count,
                    PostType = currentUser != null && p.PostMyStyles.Any(b => b.ProfileId == currentUser.Profile.Id)
                        ? PostTypeEnum.MyStyle
                        : PostTypeEnum.Feed
                });

            // Get posts (ShareCount and SharedByMe already calculated!)
            var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, request.PageNumber,
                request.PageSize);

            foreach (var post in list)
            {
                if(post.Profile != null)
                {
                    post.Profile.IsInfluencer = influencerUserIds.Contains(post.Profile.UserId);
                }           
            }

            var postsPagedResponse = _mapper.Map<PagingResponse<PostResponse>>(list);
            postsPagedResponse.ItemIds = postsPagedResponse.Items.Select(item => item.Uid).ToList();

            stopwatch.Stop();

            // ✅ Log success with timing
            _logger.LogInformation(
                "GetUserFollowingFeed completed successfully in {ElapsedMs}ms. " +
                "Returned {PostCount} posts out of {TotalCount} total. " +
                "User={UserId}, PageNumber={PageNumber}, PageSize={PageSize}",
                stopwatch.ElapsedMilliseconds,
                list.Count,
                list.TotalCount,
                currentUser.Id,
                request.PageNumber,
                request.PageSize);

            return postsPagedResponse;
        }
        catch (Exception e)
        {

            stopwatch.Stop();

            // ✅ Log error with timing
            _logger.LogError(e,
                "GetUserFollowingFeed failed after {ElapsedMs}ms. " +
                "PageNumber={PageNumber}, PageSize={PageSize}, Error={ErrorMessage}",
                stopwatch.ElapsedMilliseconds,
                request.PageNumber,
                request.PageSize,
                e.Message);

            _logger.LogError(e, "Error getting user feed with message: {message}", e.Message);
            throw;
        }
    }
}
