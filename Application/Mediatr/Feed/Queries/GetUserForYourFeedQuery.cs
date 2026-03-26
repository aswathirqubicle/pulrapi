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
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Feed.Queries;

public class GetUserForYourFeedQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
}

public class GetUserForYourFeedQueryHandler : IRequestHandler<GetUserForYourFeedQuery, PagingResponse<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetUserForYourFeedQueryHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;

    public GetUserForYourFeedQueryHandler(
        IApplicationDbContext dbContext,
        ILogger<GetUserForYourFeedQueryHandler> logger,
        IMapper mapper,
        ICurrentUserService currentUserService,
        UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<PagingResponse<PostResponse>> Handle(
        GetUserForYourFeedQuery request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "GetUserForYourFeed started for PageNumber={PageNumber}, PageSize={PageSize}",
                request.PageNumber,
                request.PageSize);

            // ============================================
            // STEP 1: Get Current User Profile
            // ============================================
            stepStopwatch.Restart();
            var currentUser = await _currentUserService.GetUserAsync();

            var currentProfile = await _dbContext.Profiles
                .Where(p => p.IsActive && p.UserId == currentUser.Id)
                .Select(p => new { p.Id, p.Uid })
                .SingleOrDefaultAsync(cancellationToken);

            if (currentProfile == null)
            {
                throw new Exception("User profile not found");
            }

            var currentProfileId = currentProfile.Id;
            var currentProfileUid = currentProfile.Uid;

            _logger.LogDebug("Step 1: Got current profile in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // STEP 2: Get Influencer Users (Batch Query)
            // ============================================
            stepStopwatch.Restart();

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

            _logger.LogDebug(
                "Step 2: Got {InfluencerCount} influencers in {ElapsedMs}ms",
                influencerUserIds.Count,
                stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // STEP 3: Build Main Query with All Filters
            // ============================================
            stepStopwatch.Restart();

            var postsQuery = _dbContext.Posts
                .AsNoTracking() // ✅ Read-only query optimization
                .Where(p => p.IsActive
                            && p.User.Id != currentUser.Id
                            && !p.User.IsSuspended

                            // ✅ Filter: Blocked users (bidirectional - both blocking and being blocked)
                            && !_dbContext.UserBlocks
                                .Where(ub => ub.IsActive &&
                                           (ub.BlockerProfileId == currentProfileUid || 
                                            ub.BlockedProfileId == currentProfileUid))
                                .Select(ub => ub.BlockerProfileId == currentProfileUid 
                                    ? ub.BlockedProfileId 
                                    : ub.BlockerProfileId)
                                .Contains(p.User.Profile.Uid)

                            // ✅ Filter: Reported posts
                            && !_dbContext.Reports
                                .Where(r => r.ReportType == ReportTypeEnum.Post)
                                .Select(r => r.EntityUid)
                                .Contains(p.Uid)

                            // ✅ Filter: Private profiles (show if public OR I'm following)
                            && (p.User.Profile.ProfileSettings == null
                                || p.User.Profile.ProfileSettings.IsProfilePublic
                                || _dbContext.ProfileFollowers
                                    .Any(pf => pf.FollowerId == currentProfileId
                                            && pf.ProfileId == p.User.Profile.Id))
                );

            _logger.LogDebug("Step 3: Built query filters in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // STEP 4: Project to PostResponse
            // ============================================
            stepStopwatch.Restart();

            var queryMapped = postsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponse()
                {
                    Uid = p.Uid,
                    ProfileUid = p.User.Profile.Uid,
                    Text = p.Text,
                    ImageWidth = p.ImageWidth,
                    ImageHeight = p.ImageHeight,
                    VideoWidth = p.VideoWidth,
                    VideoHeight = p.VideoHeight,
                    ThumbnailUrl = p.ThumbnailUrl,
                    CreatedAt = p.CreatedAt,

                    // ✅ Media File
                    MediaFile = p.MediaFile != null
                        ? new MediaFileDetailsResponse
                        {
                            Uid = p.MediaFile.Uid,
                            Url = p.MediaFile.Url,
                            FileType = p.MediaFile.MediaFileType.ToString(),
                            Priority = p.MediaFile.Priority,
                            IsHlsProcessed = p.MediaFile.IsHlsProcessed,
                            OriginalUrl = p.MediaFile.OriginalUrl,
                            HlsBasePath = p.MediaFile.HlsBasePath,
                            VideoDurationSeconds = p.MediaFile.VideoDurationSeconds,
                            AvailableQualities = p.MediaFile.AvailableQualities
                        }
                        : null,

                    // ✅ Calculate counts in database (not in foreach loop!)
                    LikesCount = p.PostLikes.Count,
                    CommentsCount = p.Comments.Count,
                    BookmarksCount = p.BookmarkCollectionItems
                        .Where(bci => bci.BookmarkCollection.IsActive)
                        .Select(bci => bci.BookmarkCollection.ProfileId)
                        .Distinct()
                        .Count(),
                    MyStylesCount = p.PostMyStyles.Count,

                    // ✅ Calculate ShareCount in database
                    ShareCount = _dbContext.Posts.Count(sp => sp.IsActive && sp.SharedPostId == p.Id),

                    // ✅ Calculate boolean flags in database
                    LikedByMe = p.PostLikes.Any(pl => pl.LikedById == currentProfileId),
                    BookmarkedByMe = p.BookmarkCollectionItems
                        .Any(bci => bci.BookmarkCollection.ProfileId == currentProfileId &&
                                   bci.BookmarkCollection.IsActive),
                    IsMyStyle = p.PostMyStyles.Any(pms => pms.ProfileId == currentProfileId),
                    SharedByMe = _dbContext.Posts.Any(sp =>
                        sp.IsActive &&
                        sp.SharedPostId == p.Id &&
                        sp.User.Profile.Id == currentProfileId),

                    // ✅ Post Type
                    PostType = p.PostMyStyles.Any(pms => pms.ProfileId == currentProfileId)
                        ? PostTypeEnum.MyStyle
                        : PostTypeEnum.Feed,

                    // ✅ Tagged Product UIDs
                    TaggedProductUids = p.PostProductTags
                        .Where(ppt => ppt.Product != null && ppt.Product.IsActive)
                        .Select(ppt => ppt.Product.Uid),

                    // ✅ Profile (if not a store post)
                    Profile = p.Store == null
                        ? new ProfileBaseResponse()
                        {
                            Uid = p.User.Profile.Uid,
                            UserId = p.User.Profile.Uid,
                            Username = p.User.UserName,
                            DisplayName = p.User.DisplayName,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            FullName = p.User.FirstName,
                            ImageUrl = p.User.Profile.ImageUrl,
                            UserType = p.User.Profile.UserType,
                            FollowedByMe = _dbContext.ProfileFollowers
                                .Any(pf => pf.FollowerId == currentProfileId &&
                                          pf.ProfileId == p.User.Profile.Id),
                            _UserId = p.User.Id // ✅ Temporary field for influencer check
                        }
                        : null,

                    // ✅ Post Product Tags
                    PostProductTags = p.PostProductTags
                        .Where(ppt => ppt.Product != null && ppt.Product.IsActive)
                        .Select(ppt => new PostProductTagResponse()
                        {
                            PositionLeftPercent = ppt.PositionLeftPercent,
                            PositionTopPercent = ppt.PositionTopPercent,
                            LocationX = ppt.LocationX,
                            LocationY = ppt.LocationY,
                            Product = new ProductPublicResponse()
                            {
                                Uid = ppt.Product.Uid,
                                Name = ppt.Product.Name,
                                WhatIsIt = ppt.Product.WhatIsIt,
                                ProductDetail = ppt.Product.ProductDetail,
                                Brand = ppt.Product.Brand,
                                MinPrice = ppt.Product.MinPrice,
                                MaxPrice = ppt.Product.MaxPrice,
                                ProductUrl = ppt.Product.ProductUrl,
                                StoreName = ppt.Product.Store.Name,
                                CountryCode = ppt.Product.Country != null ? ppt.Product.Country.Iso2 : null,
                                CurrencyCode = ppt.Product.Country != null ? ppt.Product.Country.Iso4 : null,

                                // Product Media Files
                                ProductMediaFiles = ppt.Product.ProductMediaFiles
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
                                    })
                                    .ToList(),

                                // Product Variants
                                ProductVariants = ppt.Product.ProductVariant
                                    .Select(pv => new ProductVariantResponse
                                    {
                                        VariantName = pv.VariantName,
                                        VariantOptions = pv.ProductVariantOptions
                                            .Select(opt => opt.Value)
                                            .ToList()
                                    })
                                    .ToList(),

                                // Product Owner Profile
                                Profile = new ProfileBaseResponse
                                {
                                    Uid = ppt.Product.User.Profile.Uid,
                                    UserId = ppt.Product.User.Id,
                                    ImageUrl = ppt.Product.User.Profile.ImageUrl,
                                    FullName = ppt.Product.User.FirstName,
                                    FirstName = ppt.Product.User.FirstName,
                                    LastName = ppt.Product.User.LastName,
                                    Username = ppt.Product.User.UserName,
                                    DisplayName = ppt.Product.User.DisplayName,
                                    UserType = ppt.Product.User.Profile.UserType,
                                    FollowedByMe = _dbContext.ProfileFollowers
                                        .Any(pf => pf.FollowerId == currentProfileId &&
                                                  pf.ProfileId == ppt.Product.User.Profile.Id),
                                    _UserId = ppt.Product.User.Id // ✅ For influencer check
                                }
                            }
                        })
                        .ToList(),

                    // ✅ Post Profile Mentions
                    PostProfileMentions = p.PostProfileMentions
                        .Select(ppm => new TaggedUserResponse
                        {
                            ProfileUid = ppm.Profile.Uid,
                            Username = ppm.Profile.User.UserName,
                            FirstName = ppm.Profile.User.FirstName,
                            ProfileImageUrl = ppm.Profile.ImageUrl,
                            UserType = ppm.Profile.UserType,
                            FollowedByMe = _dbContext.ProfileFollowers
                                .Any(pf => pf.FollowerId == currentProfileId &&
                                          pf.ProfileId == ppm.Profile.Id)
                        })
                        .ToList(),

                    // ✅ Post Hashtags
                    PostHashtags = p.PostHashtags
                        .Select(ph => ph.Hashtag.Value)
                        .ToList()
                });

            _logger.LogDebug("Step 4: Built projection in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // STEP 5: Execute Query and Get Paged Results
            // ============================================
            stepStopwatch.Restart();

            var list = await PagedList<PostResponse>.ToPagedListAsync(
                queryMapped,
                request.PageNumber,
                request.PageSize);

            _logger.LogDebug(
                "Step 5: Executed query in {ElapsedMs}ms, got {PostCount} posts",
                stepStopwatch.ElapsedMilliseconds,
                list.Count);

            // ============================================
            // STEP 6: Set IsInfluencer Flags (In Memory - No DB Queries!)
            // ============================================
            stepStopwatch.Restart();

            foreach (var post in list)
            {
                // Set IsInfluencer for post author
                if (post.Profile != null && post.Profile._UserId != null)
                {
                    post.Profile.IsInfluencer = influencerUserIds.Contains(post.Profile._UserId);
                    post.Profile._UserId = null; // Clear temporary field
                }

                // Set IsInfluencer for product owners
                if (post.PostProductTags != null)
                {
                    foreach (var productTag in post.PostProductTags)
                    {
                        if (productTag.Product?.Profile != null && productTag.Product.Profile._UserId != null)
                        {
                            productTag.Product.Profile.IsInfluencer = 
                                influencerUserIds.Contains(productTag.Product.Profile._UserId);
                            productTag.Product.Profile._UserId = null; // Clear temporary field
                        }
                    }
                }
            }

            _logger.LogDebug(
                "Step 6: Set influencer flags in {ElapsedMs}ms",
                stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // STEP 7: Map to Response
            // ============================================
            stepStopwatch.Restart();

            var postsPagedResponse = _mapper.Map<PagingResponse<PostResponse>>(list);
            postsPagedResponse.ItemIds = postsPagedResponse.Items.Select(item => item.Uid).ToList();

            _logger.LogDebug("Step 7: Mapped response in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

            // ============================================
            // FINAL: Log Success
            // ============================================
            stopwatch.Stop();
            _logger.LogInformation(
                "GetUserForYourFeed completed successfully in {ElapsedMs}ms. " +
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
            _logger.LogError(e,
                "GetUserForYourFeed failed after {ElapsedMs}ms. " +
                "PageNumber={PageNumber}, PageSize={PageSize}, Error={ErrorMessage}",
                stopwatch.ElapsedMilliseconds,
                request.PageNumber,
                request.PageSize,
                e.Message);
            throw;
        }
    }
}