using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Queries
{
    public class GetFeedStoriesQuery : IRequest<List<ProfileWithStoriesResponse>>
    {
        [Range(1, 20)]
        public int Limit { get; set; } = 10;

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        public FeedTypeEnum FeedType { get; set; } = FeedTypeEnum.Unset;
    }

    public class GetFeedStoriesQueryHandler : IRequestHandler<GetFeedStoriesQuery, List<ProfileWithStoriesResponse>>
    {
        private readonly ILogger<GetFeedStoriesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public GetFeedStoriesQueryHandler(ILogger<GetFeedStoriesQueryHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService,
            IUserService userService, UserManager<User> userManager,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userService = userService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<List<ProfileWithStoriesResponse>> Handle(GetFeedStoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var blockedProfileIds = new List<string>();
                if (cUser?.Profile != null)
                {
                    blockedProfileIds = await _dbContext.UserBlocks
                        .Where(ub => (ub.BlockerProfileId == cUser.Profile.Uid || ub.BlockedProfileId == cUser.Profile.Uid) && ub.IsActive)
                        .Select(ub => ub.BlockerProfileId == cUser.Profile.Uid ? ub.BlockedProfileId : ub.BlockerProfileId)
                        .ToListAsync(cancellationToken);
                }

                Expression<Func<Story, bool>> predicate = e => e.IsActive == true;
                if (cUser?.Profile != null)
                {
                    predicate = p => p.IsActive == true && p.Uid != cUser.Profile.Uid && !blockedProfileIds.Contains(p.User.Profile.Uid);
                }

                IQueryable<Story> queryableProfiles = _dbContext.Stories;

                // TODO optimize query
                // TODO, add logic based on FeedType 

                var dateTimeNow = DateTime.UtcNow;

                // Get reported story UIDs
                var reportedStoryUids = await _dbContext.Reports
                    .Where(r => r.ReportType == ReportTypeEnum.Story && r.IsActive)
                    .Select(r => r.EntityUid)
                    .ToListAsync(cancellationToken);

                var profilesWithStory = _dbContext.Stories.Where(predicate)
                    .Where(p => !p.User.IsSuspended && 
                           p.User.Stories.Where(s => s.Store == null && 
                               s.StoryExpiresIn > dateTimeNow && 
                               !reportedStoryUids.Contains(s.Uid)).Any())
                    .Select(s => s.User.Profile).Distinct()
                    // Privacy: include only public profiles or ones followed by current user
                    .Where(profile =>
                        profile.ProfileSettings == null ||
                        profile.ProfileSettings.IsProfilePublic ||
                        (cUser != null && cUser.Profile != null && _dbContext.ProfileFollowers
                            .Any(pf => pf.ProfileId == profile.Id && pf.FollowerId == cUser.Profile.Id)))
                    .OrderByDescending(p =>
                        p.User.Stories.Where(p => p.Store == null && 
                            p.StoryExpiresIn > dateTimeNow && 
                            !reportedStoryUids.Contains(p.Uid))
                            .Take(1)
                            .OrderByDescending(e => e.CreatedAt)
                            .Select(p => p.CreatedAt)
                            .FirstOrDefault())
                    .Skip((request.PageNumber - 1) * request.Limit)
                    .Take(request.Limit);

                var storyProfileList = await profilesWithStory.Select(p => new ProfileWithStoriesResponse
                {
                    Profile = new ProfileForStoryResponse
                    {
                        FullName = p.User.FirstName,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        DisplayName = p.User.DisplayName,
                        UserType = p.UserType,
                        ImageUrl = p.User.Profile.ImageUrl,
                        Uid = p.Uid,
                        UserId = p.User.Id,
                        Username = p.User.UserName,
                        LastStoryCreatedAt = p.User.Stories
                            .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                p.StoreId == null && 
                                !reportedStoryUids.Contains(p.Uid))
                            .Take(1)
                            .OrderByDescending(e => e.CreatedAt)
                            .Select(p => p.CreatedAt)
                            .FirstOrDefault(),
                        ActiveStoriesCount = p.User.Stories
                            .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                p.StoreId == null && 
                                !reportedStoryUids.Contains(p.Uid))
                            .Count(),
                        StoriesSeenCount = p.User.Stories
                            .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                p.StoreId == null && 
                                !reportedStoryUids.Contains(p.Uid))
                            .SelectMany(st => st.StorySeens)
                            .Select(seen => seen.SeenById)
                            .Distinct()
                            .Count(),
                        UnseenStoriesCount = cUser != null 
                            ? p.User.Stories.Count(p => p.StoryExpiresIn > dateTimeNow && 
                                p.StoreId == null && 
                                !reportedStoryUids.Contains(p.Uid) &&
                                !p.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                            : p.User.Stories.Count(p => p.StoryExpiresIn > dateTimeNow && 
                                p.StoreId == null && 
                                !reportedStoryUids.Contains(p.Uid)),
                    },
                    Stories = p.User.Stories
                        .Where(p => p.StoryExpiresIn > dateTimeNow && 
                            p.StoreId == null && 
                            !reportedStoryUids.Contains(p.Uid))
                        .OrderByDescending(e => e.CreatedAt)
                        .Take(request.Limit)
                        .Select(p => new StoryResponse()
                        {
                            Uid = p.Uid,                           
                            EntityUid = p.User.Profile.Uid,
                            Text = p.Text,
                            LikedByMe = cUser != null && cUser.Profile != null ? p.StoryLikes.Any(l => l.LikedById == cUser.Profile.Id) : false,
                            SeenByMe = cUser != null && cUser.Profile != null ? p.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                            LikesCount = p.StoryLikes.Count,
                            MediaFile = _mapper.Map<MediaFileDetailsResponse>(p.MediaFile),
                            PostedByStore = false,
                            CreatedAt = p.CreatedAt,
                            Colors = p.Colors,
                            TaggedProducts = p.StoryProductTags.Select(ppt =>
                            new ProductTagCoordinatesResponse
                            {
                                PositionLeftPercent = ppt.PositionLeftPercent,
                                PositionTopPercent = ppt.PositionTopPercent,
                                //Product = new ProductDetailsResponse
                                //{
                                //    AffiliateId = ppt.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                                //    ArticleCode = ppt.Product.ArticleCode,
                                //    Description = ppt.Product.Description,
                                //    Name = ppt.Product.Name,
                                //    Price = ppt.Product.Price,
                                //    Uid = ppt.Product.Uid,
                                //    PositionLeftPercent = ppt.PositionLeftPercent,
                                //    PositionTopPercent = ppt.PositionTopPercent,
                                //    ProductMediaFiles = ppt.Product.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse()
                                //    {
                                //        Uid = pmf.MediaFile.Uid,
                                //        FileType = pmf.MediaFile.MediaFileType.ToString(),
                                //        Url = pmf.MediaFile.Url,
                                //        Priority = pmf.MediaFile.Priority
                                //    })
                                //}
                            }),
                            SharedPostPreview = p.SharedPost != null ? new SharedPostPreviewDto {
                                PostUid = p.SharedPost.Uid,
                                PostOwnerUserName = p.SharedPost.User.UserName,
                                ContentPreview = p.SharedPost.Text,
                                PostOwnerImageUrl = p.SharedPost.User.Profile.ImageUrl,
                                PostImageUrl = p.SharedPost.MediaFile != null ? p.SharedPost.MediaFile.Url : null
                            } : null,
                            StoryType = p.StoryType,
                            SharedProductPreview = p.SharedProduct != null ? new SharedProductPreviewDto {
                                ProductUid = p.SharedProduct.Uid,
                                ProductName = p.SharedProduct.Name,
                                OwnerUsername = p.SharedProduct.User.UserName,
                                OwnerFullName = p.SharedProduct.User.FirstName,
                                OwnerProfileImageUrl = p.SharedProduct.User.Profile.ImageUrl,
                                WhatIsIt = p.SharedProduct.WhatIsIt,
                                ProductDetail = p.SharedProduct.ProductDetail,
                                MinPrice = p.SharedProduct.MinPrice,
                                MaxPrice = p.SharedProduct.MaxPrice,
                                CurrencyCode = p.SharedProduct.Country != null ? p.SharedProduct.Country.Iso4 : null,
                                //ProductImageUrl = p.SharedProduct.ProductMediaFiles
                                //    .OrderBy(pmf => pmf.MediaFile.Priority)
                                //    .FirstOrDefault()?.MediaFile.Url,
                                ImageUrls = p.SharedProduct.ProductMediaFiles.Select(pmf => pmf.MediaFile.Url).ToList()
                            } : null,
                            SharedCollectionPreview = p.SharedCollection != null ? new SharedCollectionPreviewDto {
                                CollectionUid = p.SharedCollection.Uid,
                                OwnerUsername = p.SharedCollection.Profile.User.UserName,
                                OwnerProfileImageUrl = p.SharedCollection.Profile.ImageUrl,
                                CollectionName = p.SharedCollection.Name,
                                TotalPostCount = p.SharedCollection.BookmarkCollectionItems.Count,
                                First4PostImageUrls = p.SharedCollection.BookmarkCollectionItems
                                    .OrderByDescending(bci => bci.CreatedAt)
                                    .Take(4)
                                    .Select(bci => bci.Post.MediaFile.Url)
                                    .ToList()
                            } : null,
                            VideoWidth = p.VideoWidth,
                            VideoHeight = p.VideoHeight,
                        })
                }
                    ).Take(request.Limit / 2)
                    .ToListAsync(cancellationToken);


                if (storyProfileList.Any() && cUser != null)
                {
                    List<string> storyProfileListUids = storyProfileList.Select(sp => sp.Profile.Uid).ToList();

                    var myFollows = await _dbContext.ProfileFollowers
                        .Where(pf => pf.FollowerId == cUser.Profile.Id && storyProfileListUids.Contains(pf.Profile.Uid))
                        .Select(pf => pf.Profile.Uid).ToListAsync(cancellationToken);

                    foreach (var item in storyProfileList)
                    {
                        item.Profile.FollowedByMe = myFollows.Contains(item.Profile.Uid);
                        item.Profile.IsInfluencer = await _userManager.IsInRoleAsync(new User() { Id = item.Profile.UserId }, PulrRoles.Influencer);
                    }
                }

                IQueryable<Store> queryableStores = _dbContext.Stores;
                var storyStoreList = await queryableStores
                    .Where(s => !s.User.IsSuspended && s.IsActive && s.Stories.Where(p => p.Store != null && p.StoryExpiresIn > dateTimeNow).Any())
                    .OrderByDescending(s =>
                        s.Stories.Where(p => p.Store != null && p.StoryExpiresIn > dateTimeNow).Take(1).OrderByDescending(e => e.CreatedAt).Select(p => p.CreatedAt)
                            .FirstOrDefault())
                    .Skip((request.PageNumber - 1) * request.Limit)
                    .Take(request.Limit).Select(s => new ProfileWithStoriesResponse()
                    {
                        Profile = new ProfileForStoryResponse()
                        {
                            StoreName = s.Name,
                            StoreImageUrl = s.ImageUrl,
                            StoreUid = s.Uid,
                            StoreUniqueName = s.UniqueName,
                            IsStore = true,
                            LastStoryCreatedAt = s.Stories.Take(1).OrderByDescending(e => e.CreatedAt).Select(p => p.CreatedAt).FirstOrDefault(),
                            ActiveStoriesCount = s.Stories
                                .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                    !reportedStoryUids.Contains(p.Uid))
                                .Count(),
                            StoriesSeenCount = s.Stories
                                .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                    !reportedStoryUids.Contains(p.Uid))
                                .SelectMany(st => st.StorySeens)
                                .Select(seen => seen.SeenById)
                                .Distinct()
                                .Count(),
                            UnseenStoriesCount = cUser != null 
                                ? s.Stories.Count(p => p.StoryExpiresIn > dateTimeNow && 
                                    !reportedStoryUids.Contains(p.Uid) &&
                                    !p.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                                : s.Stories.Count(p => p.StoryExpiresIn > dateTimeNow && 
                                    !reportedStoryUids.Contains(p.Uid)),
                        },
                        // TODO: check with team limit for stories, for now limited to 10 cause of large query
                        Stories = s.Stories
                            .Where(p => p.StoryExpiresIn > dateTimeNow && 
                                !reportedStoryUids.Contains(p.Uid))
                            .OrderByDescending(e => e.CreatedAt)
                            .Take(request.Limit)
                            .Select(p => new StoryResponse()
                        {
                            Uid = p.Uid,
                            EntityUid = s.Uid,
                            Text = p.Text,
                            LikedByMe = cUser != null && cUser.Profile != null ? p.StoryLikes.Any(l => l.LikedById == cUser.Profile.Id) : false,
                            SeenByMe = cUser != null && cUser.Profile != null ? p.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                            LikesCount = p.StoryLikes.Count,
                            MediaFile = _mapper.Map<MediaFileDetailsResponse>(p.MediaFile),
                            PostedByStore = true,
                            TaggedProducts = p.StoryProductTags.Select(ppt =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = ppt.PositionLeftPercent,
                                    PositionTopPercent = ppt.PositionTopPercent,
                                    //Product = new ProductDetailsResponse()
                                    //{
                                    //    AffiliateId = ppt.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                                    //    ArticleCode = ppt.Product.ArticleCode,
                                    //    Description = ppt.Product.Description,
                                    //    Name = ppt.Product.Name,
                                    //    Price = ppt.Product.Price,
                                    //    Uid = ppt.Product.Uid,
                                    //    ProductMediaFiles = ppt.Product.ProductMediaFiles.Select(pmf => new MediaFileDetailsResponse()
                                    //    {
                                    //        Uid = pmf.MediaFile.Uid,
                                    //        FileType = pmf.MediaFile.MediaFileType.ToString(),
                                    //        Url = pmf.MediaFile.Url,
                                    //        Priority = pmf.MediaFile.Priority
                                    //    })
                                    //}
                                }),
                            SharedPostPreview = p.SharedPost != null ? new SharedPostPreviewDto {
                                PostUid = p.SharedPost.Uid,
                                PostOwnerUserName = p.SharedPost.User.UserName,
                                ContentPreview = p.SharedPost.Text,
                                PostOwnerImageUrl = p.SharedPost.User.Profile.ImageUrl,
                                PostImageUrl = p.SharedPost.MediaFile != null ? p.SharedPost.MediaFile.Url : null
                            } : null,
                            StoryType = p.StoryType,
                            SharedProductPreview = p.SharedProduct != null ? new SharedProductPreviewDto {
                                ProductUid = p.SharedProduct.Uid,
                                ProductName = p.SharedProduct.Name,
                                OwnerUsername = p.SharedProduct.User.UserName,
                                OwnerFullName = p.SharedProduct.User.FirstName,
                                OwnerProfileImageUrl = p.SharedProduct.User.Profile.ImageUrl,
                                WhatIsIt = p.SharedProduct.WhatIsIt,
                                ProductDetail = p.SharedProduct.ProductDetail,
                                MinPrice = p.SharedProduct.MinPrice,
                                MaxPrice = p.SharedProduct.MaxPrice,
                                CurrencyCode = p.SharedProduct.Country != null ? p.SharedProduct.Country.Iso4 : null,
                                CountryCode = p.SharedProduct.Country != null ? p.SharedProduct.Country.Iso2 : null,
                                ProductImageUrl = p.SharedProduct.ProductMediaFiles
                                    .OrderBy(pmf => pmf.MediaFile.Priority)
                                    .FirstOrDefault() != null ? p.SharedProduct.ProductMediaFiles
                                    .OrderBy(pmf => pmf.MediaFile.Priority)
                                    .FirstOrDefault().MediaFile.Url : null,
                                ImageUrls = p.SharedProduct.ProductMediaFiles.Select(pmf => pmf.MediaFile.Url).ToList()
                            } : null,
                            SharedCollectionPreview = p.SharedCollection != null ? new SharedCollectionPreviewDto {
                                CollectionUid = p.SharedCollection.Uid,
                                OwnerUsername = p.SharedCollection.Profile.User.UserName,
                                OwnerProfileImageUrl = p.SharedCollection.Profile.ImageUrl,
                                CollectionName = p.SharedCollection.Name,
                                TotalPostCount = p.SharedCollection.BookmarkCollectionItems.Count,
                                First4PostImageUrls = p.SharedCollection.BookmarkCollectionItems
                                    .OrderByDescending(bci => bci.CreatedAt)
                                    .Take(4)
                                    .Select(bci => bci.Post.MediaFile.Url)
                                    .ToList()
                            } : null,
                            CreatedAt = p.CreatedAt,
                            VideoWidth = p.VideoWidth,
                            VideoHeight = p.VideoHeight,
                        })
                    }
                    )
                    .Take(request.Limit / 2)
                    .ToListAsync(cancellationToken);

                if (storyStoreList.Any() && cUser != null)
                {
                    List<string> storyStoreListUids = storyStoreList.Select(p => p.Profile.Uid).ToList();

                    var myStoreFollows = await _dbContext.StoreFollowers
                        .Where(sf => sf.FollowerId == cUser.Profile.Id && storyStoreListUids.Contains(sf.Store.Uid))
                        .Select(sf => sf.Store.Uid).ToListAsync(cancellationToken);

                    foreach (var item in storyStoreList)
                    {
                        item.Profile.FollowedByMe = myStoreFollows.Contains(item.Profile.Uid);
                    }
                }

                ;
                //// shuffle list, might be useful later
                ////return list1.OrderBy(x => Random.Shared.Next()).ToList();
                storyProfileList.AddRange(storyStoreList);


                storyProfileList.ForEach(s => { s.Profile.StoryUids.AddRange(s.Stories.Select(s => s.Uid).ToList()); });

                return storyProfileList.OrderByDescending(item => item.Profile.LastStoryCreatedAt).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}