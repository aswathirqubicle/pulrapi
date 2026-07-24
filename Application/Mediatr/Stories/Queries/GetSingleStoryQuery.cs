using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Queries;

public class GetSingleStoryQuery : IRequest<ProfileWithStoriesResponse>
{
    public string Uid { get; set; }
}

public class GetSingleStoryQueryHandler : IRequestHandler<GetSingleStoryQuery, ProfileWithStoriesResponse>
{
    private readonly ILogger<GetSingleStoryQueryHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public GetSingleStoryQueryHandler(ILogger<GetSingleStoryQueryHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<ProfileWithStoriesResponse> Handle(GetSingleStoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var now = DateTime.UtcNow;
            var story = await _dbContext.Stories
                .Include(s => s.StorySeens)
                .Include(s => s.StoryLikes)
                .Include(s => s.MediaFile)
                .Include(s => s.StoryProductTags).ThenInclude(pt => pt.Product).ThenInclude(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                .Include(s => s.User).ThenInclude(u => u.Profile).ThenInclude(p => p.ProfileSettings)
                .Include(s => s.Store)
                .Include(s => s.SharedPost).ThenInclude(p => p.User).ThenInclude(u => u.Profile)
                .Include(s => s.SharedPost).ThenInclude(p => p.MediaFile)
                .Include(s => s.SharedProduct).ThenInclude(p => p.User).ThenInclude(u => u.Profile)
                .Include(s => s.SharedProduct).ThenInclude(p => p.Country)
                .Include(s => s.SharedProduct).ThenInclude(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                .Include(s => s.SharedCollection).ThenInclude(c => c.Profile).ThenInclude(pr => pr.User)
                .Include(s => s.SharedCollection).ThenInclude(c => c.BookmarkCollectionItems).ThenInclude(bci => bci.Post).ThenInclude(p => p.MediaFile)
                .FirstOrDefaultAsync(s => s.IsActive && s.Uid == request.Uid && s.StoryExpiresIn > now, cancellationToken);

            if (story == null)
                return null;

            // Privacy check: block non-followers from viewing stories of private profiles
            if (story.Store == null && story.User?.Profile != null)
            {
                var ownerProfile = story.User.Profile;
                bool isPublic = ownerProfile.ProfileSettings == null || ownerProfile.ProfileSettings.IsProfilePublic;
                if (!isPublic)
                {
                    var currentProfileId = currentUser?.Profile?.Id;
                    bool isOwner = currentProfileId.HasValue && ownerProfile.Id == currentProfileId.Value;
                    if (!isOwner)
                    {
                        bool isFollower = currentProfileId.HasValue &&
                            await _dbContext.ProfileFollowers.AnyAsync(
                                pf => pf.ProfileId == ownerProfile.Id && pf.FollowerId == currentProfileId.Value, cancellationToken);
                        if (!isFollower)
                            throw new ForbiddenException("This profile is private.");
                    }
                }
            }

            var storyResponse = new StoryResponse
            {
                Uid = story.Uid,
                EntityUid = story.Store != null ? story.Store.Uid : story.User?.Profile?.Uid,
                Text = story.Text,
                LikedByMe = currentUser != null && currentUser.Profile != null
                    ? story.StoryLikes.Any(l => l.LikedById == currentUser.Profile.Id)
                    : false,
                SeenByMe = currentUser != null && currentUser.Profile != null
                    ? story.StorySeens.Any(s => s.SeenById == currentUser.Profile.Id)
                    : false,
                LikesCount = story.StoryLikes.Count,
                MediaFile = _mapper.Map<MediaFileDetailsResponse>(story.MediaFile),
                PostedByStore = story.Store != null,
                StoryType = story.StoryType,
                TaggedProducts = story.StoryProductTags.Select(stp =>
                    new ProductTagCoordinatesResponse
                    {
                        PositionLeftPercent = stp.PositionLeftPercent,
                        PositionTopPercent = stp.PositionTopPercent,
                        //Product = new ProductDetailsResponse
                        //{
                        //    AffiliateId = stp.Product.OrderProductAffiliate?.Affiliate?.AffiliateId,
                        //    Name = stp.Product.Name,
                        //    Price = stp.Product.Price,
                        //    Uid = stp.Product.Uid,
                        //    ProductMediaFiles = stp.Product.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse
                        //    {
                        //        Uid = pmf.MediaFile.Uid,
                        //        FileType = pmf.MediaFile.MediaFileType.ToString(),
                        //        Url = pmf.MediaFile.Url,
                        //        Priority = pmf.MediaFile.Priority
                        //    })
                        //}
                    }),
                CommentsCount = 0, // If you have comments, fetch and count here
                CreatedAt = story.CreatedAt,
                SharedPostPreview = story.SharedPost != null ? new SharedPostPreviewDto {
                    PostUid = story.SharedPost.Uid,
                    PostOwnerUserName = story.SharedPost.User?.UserName,
                    ContentPreview = story.SharedPost.Text,
                    PostOwnerImageUrl = story.SharedPost.User?.Profile?.ImageUrl,
                    PostImageUrl = story.SharedPost.MediaFile != null ? story.SharedPost.MediaFile.Url : null
                } : null,
                SharedProductPreview = story.SharedProduct != null ? new SharedProductPreviewDto {
                    ProductUid = story.SharedProduct.Uid,
                    ProductName = story.SharedProduct.Name,
                    OwnerUsername = story.SharedProduct.User?.UserName,
                    OwnerFullName = story.SharedProduct.User?.FirstName,
                    OwnerProfileImageUrl = story.SharedProduct.User?.Profile?.ImageUrl,
                    WhatIsIt = story.SharedProduct.WhatIsIt,
                    ProductDetail = story.SharedProduct.ProductDetail,
                    MinPrice = story.SharedProduct.MinPrice,
                    MaxPrice = story.SharedProduct.MaxPrice,
                    CurrencyCode = story.SharedProduct.Country != null ? story.SharedProduct.Country.Iso4 : null,
                    ProductImageUrl = story.SharedProduct.ProductMediaFiles
                        .OrderBy(pmf => pmf.MediaFile.Priority)
                        .FirstOrDefault()?.MediaFile.Url,
                    ImageUrls = story.SharedProduct.ProductMediaFiles.Select(pmf => pmf.MediaFile.Url).ToList(),
                } : null,
                SharedCollectionPreview = story.SharedCollection != null ? new SharedCollectionPreviewDto {
                    CollectionUid = story.SharedCollection.Uid,
                    OwnerUsername = story.SharedCollection.Profile?.User?.UserName,
                    OwnerProfileImageUrl = story.SharedCollection.Profile?.ImageUrl,
                    CollectionName = story.SharedCollection.Name,
                    TotalPostCount = story.SharedCollection.BookmarkCollectionItems.Count,
                    First4PostImageUrls = story.SharedCollection.BookmarkCollectionItems
                        .OrderByDescending(bci => bci.CreatedAt)
                        .Take(4)
                        .Select(bci => bci.Post.MediaFile.Url)
                        .ToList()
                } : null,
                Colors = story.Colors,
                VideoWidth = story.VideoWidth,
                VideoHeight = story.VideoHeight
            };

            ProfileForStoryResponse profile;
            if (story.Store != null)
            {
                profile = new ProfileForStoryResponse
                {
                    StoreName = story.Store.Name,
                    StoreImageUrl = story.Store.ImageUrl,
                    StoreUid = story.Store.Uid,
                    StoreUniqueName = story.Store.UniqueName,
                    IsStore = true,
                    LastStoryCreatedAt = story.CreatedAt,
                    StoryUids = new List<string> { story.Uid }
                };
            }
            else
            {
                profile = new ProfileForStoryResponse
                {
                    FullName = story.User?.FirstName,
                    FirstName = story.User?.FirstName,
                    LastName = story.User?.LastName,
                    DisplayName = story.User?.DisplayName,
                    UserType = story.User?.Profile?.UserType,
                    ImageUrl = story.User?.Profile?.ImageUrl,
                    Uid = story.User?.Profile?.Uid,
                    UserId = story.User?.Id,
                    Username = story.User?.UserName,
                    LastStoryCreatedAt = story.CreatedAt,
                    StoryUids = new List<string> { story.Uid }
                };
            }

            // Optionally: set FollowedByMe and IsInfluencer if needed (requires extra queries)

            return new ProfileWithStoriesResponse
            {
                Profile = profile,
                Stories = new List<StoryResponse> { storyResponse }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting single story");
            throw;
        }
    }
}