using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.Services
{
    public class PostService : IPostService
    {
        private readonly ILogger<PostService> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public PostService(
            ILogger<PostService> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IFileUploadService fileUploadService,
            IConfiguration configuration,
            IQueryHelperService queryHelperService,
            IExchangeRateService exchangeRateService,
            IMapper mapper,
            INotificationService notificationService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _fileUploadService = fileUploadService;
            _configuration = configuration;
            _queryHelperService = queryHelperService;
            _exchangeRateService = exchangeRateService;
            _mapper = mapper;
            _notificationService = notificationService;
        }


        public async Task<PagingResponse<PostResponse>> GetPosts(GetPostsQueryParams queryParams)
        {
            try
            {
                IQueryable<Post> query = _dbContext.Posts.Where(p => !p.User.IsSuspended);

                var cUser = await _currentUserService.GetUserAsync(false, true);
                Store store = null;

                // Filter out posts from blocked users (bidirectional: both blocking and being blocked)
                if (cUser != null)
                {
                    var blockedByMe = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockerProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockedProfileId)
                        .ToListAsync();

                    var blockedMe = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockedProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockerProfileId)
                        .ToListAsync();

                    var allBlockedProfileIds = blockedByMe.Union(blockedMe).ToList();

                    query = query.Where(p => !allBlockedProfileIds.Contains(p.User.Profile.Uid) && p.IsActive);

                    // Filter out posts reported by current user
                    var reportedPostIds = await _dbContext.Reports
                        .Where(r => r.ReportType == ReportTypeEnum.Post 
                            && r.IsActive 
                            && r.ReportedById == cUser.Id)
                        .Select(r => r.EntityUid)
                        .ToListAsync();

                    _logger.LogInformation($"Found {reportedPostIds.Count} reported posts for user {cUser.Id}");
                    
                    if (reportedPostIds.Any())
                    {
                        query = query.Where(p => !reportedPostIds.Contains(p.Uid));
                        _logger.LogInformation("Applied reported posts filter");
                    }

                    // Filter out posts from private profiles that the current user is not following
                    var privateProfileIds = await _dbContext.Profiles
                        .Where(p => !p.ProfileSettings.IsProfilePublic)
                        .Select(p => p.Id)
                        .ToListAsync();

                    var followingProfileIds = await _dbContext.ProfileFollowers
                        .Where(pf => pf.FollowerId == cUser.Profile.Id)
                        .Select(pf => pf.ProfileId)
                        .ToListAsync();

                    // Allow posts from private profiles if:
                    // 1. The current user is following the profile, OR
                    // 2. The current user is the owner of the profile (can see their own posts)
                    query = query.Where(p => 
                        !privateProfileIds.Contains(p.User.Profile.Id) || 
                        followingProfileIds.Contains(p.User.Profile.Id) ||
                        p.User.Profile.Id == cUser.Profile.Id); // Allow profile owner to see their own posts
                }
                else
                {
                    // If not logged in, only show posts from public profiles or where IsProfilePublic is null
                    query = query.Where(p => p.IsActive && (p.User.Profile.ProfileSettings.IsProfilePublic == true));
                }

                // we calculate here, cause ef core doesnt know how to compare column value with DateTime.UtcNow directly 
                var datetimeNow = DateTime.UtcNow;

                if (!String.IsNullOrWhiteSpace(queryParams.Search))
                {
                    if (queryParams.Search.StartsWith("#"))
                    {
                        var searchWithoutHashtag = queryParams.Search.Replace("#", "");
                        query = query.Where(p =>
                            p.PostHashtags.Any(ph => EF.Functions.Like(ph.Hashtag.Value, $"%{searchWithoutHashtag}%")));
                    }
                    else
                    {
                        query = query.Where(p =>
                            EF.Functions.Like(p.User.UserName, $"%{queryParams.Search}%") ||
                            EF.Functions.Like(p.Store.UniqueName, $"%{queryParams.Search}%"));
                    }
                }

                if (!string.IsNullOrWhiteSpace(queryParams.Tags))
                {
                    var tagsList = queryParams.Tags.Split(",").Select(t => t.ToLower()).ToList();
                    var tagsListWithHashtag = tagsList.Select(t => $"#{t}").ToList();
                    query = query.Where(p => p.PostHashtags.Any(ph => tagsListWithHashtag.Contains(ph.Hashtag.Value.ToLower())));
                }

                //TODO rewrite this filter based on new categories structure
                /*if (!String.IsNullOrWhiteSpace(queryParams.Categories))
                {
                    var categorySlugList = queryParams.Categories.Split(',').ToList();
                    query = query.Where(p => p.PostProductTags != null && p.PostProductTags.Any(ppt =>
                        ppt.Product.ProductCategory != null &&
                        categorySlugList.Contains(ppt.Product.ProductCategory.Category.Slug)));
                }*/


                if (queryParams.ProfileType == ProfileTypeEnum.Store &&
                    !String.IsNullOrWhiteSpace(queryParams.EntityUid))
                {
                    store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.Uid == queryParams.EntityUid);
                    query = query.Where(p => p.Store.Uid == queryParams.EntityUid);
                }
                else if (queryParams.ProfileType == ProfileTypeEnum.Profile &&
                         !String.IsNullOrWhiteSpace(queryParams.EntityUid))
                {
                    // Try to find profile by UID first
                    var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.Uid == queryParams.EntityUid);
                    string profileUid = queryParams.EntityUid;
                    if (profile == null)
                    {
                        // If not found, try by username
                        profile = await _dbContext.Profiles.Include(p => p.User).FirstOrDefaultAsync(p => p.User.UserName == queryParams.EntityUid);
                        if (profile != null)
                        {
                            profileUid = profile.Uid;
                        }
                    }
                    if (profile != null)
                    {
                        query = query.Where(p => p.User.Profile.Uid == profileUid);
                        _logger.LogInformation($"Filtering posts for profile {profileUid}");
                    }
                }

                //filter by hashtag 
                if (!String.IsNullOrWhiteSpace(queryParams.Hashtag))
                {
                    query = query.Where(p => p.PostHashtags.Any(ph => ph.Hashtag.Value == queryParams.Hashtag));
                }

                // Filter out posts reported by current user after profile filtering
                if (cUser != null)
                {
                    var reportedPostIds = await _dbContext.Reports
                        .Where(r => r.ReportType == ReportTypeEnum.Post 
                            && r.IsActive 
                            && r.ReportedById == cUser.Id)
                        .Select(r => r.EntityUid)
                        .ToListAsync();

                    if (reportedPostIds.Any())
                    {
                        query = query.Where(p => !reportedPostIds.Contains(p.Uid));
                    }
                }

                else if (queryParams.PostType == PostTypeEnum.Product)
                {
                    query = query.Where(p => p.PostProductTags.Any());
                }

                // Filter by product type if provided
                if (queryParams.ProductType.HasValue)
                {
                    query = query.Where(p => p.PostProductTags.Any(ppt => ppt.Product.Type == queryParams.ProductType.Value));
                }

                if (!String.IsNullOrWhiteSpace(queryParams.Order) && !String.IsNullOrWhiteSpace(queryParams.OrderBy))
                {
                    query = _queryHelperService.AppendOrderBy(query, queryParams.OrderBy, queryParams.Order);
                }
                else if (queryParams.SortingLogic == PostSortingLogicEnum.Trending)
                {
                    query = query.OrderByDescending(e => e.PostLikes.Count())
                        .ThenByDescending(e => e.Comments.Count())
                        .ThenByDescending(e => e.PostClicks.Where(pc => pc.User != null).Count())
                        .ThenByDescending(e => e.PostClicks.Where(pc => pc.User == null).Count());
                }
                else
                {
                    query = query.OrderByDescending(u => u.CreatedAt);
                }

                List<string> currencyCodes = null;
                //string storeCurrencyCode = null;
                List<ExchangeRate> exchangeRates = null;
                Currency currency = null;

                if (queryParams.CurrencyCode != null)
                {
                    exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);
                    currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.IsActive && c.Code == queryParams.CurrencyCode);
                }
                else
                {
                    currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]);
                }

                var queryMapped = query
                    .Select(c => new PostResponse
                    {
                        Uid = c.Uid,
                        //StoreUid = c.Store != null ? c.Store.Uid : null,
                        ProfileUid = c.Store == null ? c.User.Profile.Uid : null,
                        Text = c.Text,
                        ImgDescription = c.ImgDescription,
                        ThumbnailUrl = string.IsNullOrEmpty(c.ThumbnailUrl) ? (c.MediaFile != null ? (c.MediaFile.OriginalUrl ?? c.MediaFile.Url) : null) : c.ThumbnailUrl,
                        MediaFile = _mapper.Map<MediaFileDetailsResponse>(c.MediaFile),
                        LikesCount = c.PostLikes.Count(),
                        LikedByMe = cUser != null && c.PostLikes.Any(pl => pl.LikedById == cUser.Profile.Id),
                        TaggedProductUids = c.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => ppt.Product.Uid),
                        CreatedAt = c.CreatedAt,
                        ImageWidth = c.ImageWidth,
                        ImageHeight = c.ImageHeight,
                        VideoWidth = c.VideoWidth,
                        VideoHeight = c.VideoHeight,
                        //PostedByStore = c.Store != null,
                        PostProfileMentions = c.PostProfileMentions.Select(e => new TaggedUserResponse
                        {
                            ProfileUid = e.Profile.Uid,
                            Username = e.Profile.User.UserName,
                            FirstName = e.Profile.User.FirstName,
                            ProfileImageUrl = e.Profile.ImageUrl,
                            FollowedByMe = cUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id),
                            UserType = e.Profile.UserType
                        }).ToList(),
                        //PostStoreMentions = c.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                        PostHashtags = c.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                        BookmarkedByMe = cUser != null && c.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == cUser.Profile.Id && bci.BookmarkCollection.IsActive),
                        BookmarksCount = c.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
                        MyStylesCount = c.PostMyStyles.Count,
                        IsMyStyle = cUser != null && c.PostMyStyles.Any(ms => ms.ProfileId == cUser.Profile.Id),
                        CommentsCount = c.Comments.Count(comment => comment.IsActive),
                        //Store = c.Store != null ? new StoreBaseResponse()
                        //{
                        //    Uid = c.Store.Uid,
                        //    Name = c.Store.Name,
                        //    ImageUrl = c.Store.ImageUrl,
                        //    UniqueName = c.Store.UniqueName,
                        //    CurrencyCode = c.Store.Currency.Code,
                        //    FollowedByMe = cUser != null && c.Store.StoreFollowers.Any(sf => sf.FollowerId == cUser.Profile.Id),
                        //} : null,
                        PostProductTags = c.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse()
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
                                CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
                                CurrencyCode = e.Product.Country != null ? e.Product.Country.Iso4 : null,
                                StoreName = e.Product.Store.Name,
                                Type = e.Product.Type,
                                ProductMediaFiles = e.Product.ProductMediaFiles
                                .Where(pmf => pmf.MediaFile.IsActive)
                                .Select(pmf => new MediaFileDetailsResponse
                                {
                                    Uid = pmf.MediaFile.Uid,
                                    Url = pmf.MediaFile.Url,
                                    FileType = pmf.MediaFile.MediaFileType.ToString()
                                }).ToList(),
                                ProductVariants = e.Product.ProductVariant
                                    .Select(pv => new ProductVariantResponse
                                    {
                                        VariantName = pv.VariantName,
                                        VariantOptions = pv.ProductVariantOptions.Select(opt => opt.Value).ToList()
                                    }).ToList(),
                                Profile = new ProfileBaseResponse
                                {
                                    Uid = e.Product.User.Profile.Uid,
                                    UserId = e.Product.User.Id,
                                    ImageUrl = e.Product.User.Profile.ImageUrl,
                                    //IsStore = false,
                                    FullName = e.Product.User.FirstName,
                                    FirstName = e.Product.User.FirstName,
                                    LastName = e.Product.User.LastName,
                                    Username = e.Product.User.UserName,
                                    DisplayName = e.Product.User.DisplayName,
                                    UserType = e.Product.User.Profile.UserType,
                                    FollowedByMe = cUser != null && e.Product.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id)
                                }
                            },
                            PositionLeftPercent = e.PositionLeftPercent,
                            PositionTopPercent = e.PositionTopPercent,
                            LocationX = e.LocationX,
                            LocationY = e.LocationY
                        }).ToList(),
                        Profile = c.Store == null
                            ? new ProfileBaseResponse()
                            {
                                Uid = c.User.Profile.Uid,
                                FullName = c.User.FirstName,
                                FirstName = c.User.FirstName,
                                LastName = c.User.LastName,
                                ImageUrl = c.User.Profile.ImageUrl,
                                Username = c.User.UserName,
                                DisplayName = c.User.DisplayName,
                                UserType = c.User.Profile.UserType,
                                FollowedByMe = cUser != null
                                    ? c.User.Profile.ProfileFollowers.Any(e => e.FollowerId == cUser.Profile.Id)
                                    : false,
                            }
                            : null,
                        PostType = PostTypeEnum.Feed,
                    });

                //var queryRaw = queryMapped.ToSql();
                var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, queryParams.PageNumber,
                    queryParams.PageSize);

                //foreach (var item in list)
                //{
                //    if (item.Store != null)
                //    {
                //        if (cUser != null)
                //        {
                //            item.PostType = PostTypeEnum.Feed;
                //        }

                //        item.Store.IsMyStore = cUser?.Stores.Any(store => store.Uid == item.Store.Uid) ?? false;
                //    }
                //}
                var res = _mapper.Map<PagingResponse<PostResponse>>(list);
                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<ToggleLikePostDto> PostToggleLike(string postUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.Uid == postUid);

                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"Profile doesnt exist for user '{cUser.Id}' .");
                }

                if (post == null)
                {
                    throw new BadRequestException($"Post with uid {postUid} doesnt exist.");
                }

                var existingPostLike = await _dbContext.PostLikes
                    .Include(pl => pl.Post)
                    .SingleOrDefaultAsync(l => l.Post.Uid == postUid && l.LikedBy.Uid == cUser.Profile.Uid);

                var likedByMe = false;
                if (existingPostLike == null)
                {
                    _dbContext.PostLikes.Add(new PostLike() { Post = post, LikedBy = cUser.Profile });
                    await _notificationService.SaveLikeNotificationAsync(cUser.Id, postUid, EntityTypeEnum.POST, ActivityActionTypeEnum.LikePost);
                    likedByMe = true;
                }
                else
                {
                    _dbContext.PostLikes.Remove(existingPostLike);

                    // Delete the like notification history
                    var notification = await _dbContext.NotificationHistories
                        .FirstOrDefaultAsync(n =>
                            n.TargetId == postUid &&
                            n.TargetType == EntityTypeEnum.POST &&
                            n.ActionType == NotificationActionTypeEnum.Like &&
                            n.ActorUserId == cUser.Profile.Id);

                    if (notification != null)
                    {
                        _dbContext.NotificationHistories.Remove(notification);
                    }
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return new ToggleLikePostDto()
                {
                    LikedByMe = likedByMe,
                    LikesCount = await _dbContext.PostLikes
                        .Where(pl => pl.PostId == post.Id).CountAsync()
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task ToggleToMyStyle(string postUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser?.Profile == null)
                {
                    throw new BadRequestException($"User {cUser?.UserName} doesnt have a profile.");
                }

                ;

                var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.Uid == postUid);
                if (post == null)
                {
                    throw new BadRequestException($"Post with uid {postUid} doesnt exist.");
                }

                var postMyStyle = await _dbContext.PostMyStyles.SingleOrDefaultAsync(pms =>
                    pms.Post.Id == post.Id && pms.Profile.Id == cUser.Profile.Id);

                if (postMyStyle == null)
                {
                    _dbContext.PostMyStyles.Add(new PostMyStyle() { Post = post, Profile = cUser.Profile });
                }
                else
                {
                    _dbContext.PostMyStyles.Remove(postMyStyle);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<PagingResponse<PostResponse>> GetPostsMyStyle(GetPostsQueryParams queryParams)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<PostMyStyle> query = _dbContext.PostMyStyles;

                query = query.Where(pms => pms.Profile.Uid == queryParams.EntityUid && !pms.Profile.User.IsSuspended);

                // Filter out posts from blocked users (bidirectional: both blocking and being blocked)
                if (cUser != null)
                {
                    var blockedByMe = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockerProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockedProfileId)
                        .ToListAsync();

                    var blockedMe = await _dbContext.UserBlocks
                        .Where(ub => ub.BlockedProfileId == cUser.Profile.Uid && ub.IsActive)
                        .Select(ub => ub.BlockerProfileId)
                        .ToListAsync();

                    var allBlockedProfileIds = blockedByMe.Union(blockedMe).ToList();

                    query = query.Where(pms => !allBlockedProfileIds.Contains(pms.Post.User.Profile.Uid) && pms.IsActive);
                }

                // Filter out reported posts
                var reportedPostIds = await _dbContext.Reports
                    .Where(r => r.ReportType == ReportTypeEnum.Post && r.IsActive)
                    .Select(r => r.EntityUid)
                    .ToListAsync();

                query = query.Where(pms => !reportedPostIds.Contains(pms.Post.Uid));

                var queryMapped = query
                    .Select(pms => new PostResponse()
                    {
                        Uid = pms.Post.Uid,
                        // TODO -> same for my styles for store
                        //StoreUid = pms.Post.Store != null ? pms.Post.Store.Uid : null,
                        ProfileUid = pms.Post.Store == null ? pms.Post.User.Profile.Uid : null,
                        Text = pms.Post.Text,
                        ImgDescription = pms.Post.ImgDescription,
                        ThumbnailUrl = string.IsNullOrEmpty(pms.Post.ThumbnailUrl) ? (pms.Post.MediaFile != null ? (pms.Post.MediaFile.OriginalUrl ?? pms.Post.MediaFile.Url) : null) : pms.Post.ThumbnailUrl,
                        MediaFile = _mapper.Map<MediaFileDetailsResponse>(pms.Post.MediaFile),
                        LikesCount = pms.Post.PostLikes.Count(),
                        LikedByMe = cUser != null
                            ? pms.Post.PostLikes.Any(pl => pl.LikedBy.Uid == cUser.Profile.Uid)
                            : false,
                        TaggedProductUids = pms.Post.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => ppt.Product.Uid),
                        PostProductTags = pms.Post.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse
                        {
                            PositionLeftPercent = e.PositionLeftPercent,
                            PositionTopPercent = e.PositionTopPercent,
                            LocationX = e.LocationX,
                            LocationY = e.LocationY,
                            Product = new ProductPublicResponse
                            {
                                Uid = e.Product.Uid,
                                Name = e.Product.Name,
                                WhatIsIt = e.Product.WhatIsIt,
                                ProductDetail = e.Product.ProductDetail,
                                Brand = e.Product.Brand,
                                MinPrice = e.Product.MinPrice,
                                MaxPrice = e.Product.MaxPrice,
                                ProductUrl = e.Product.ProductUrl,
                                StoreName = e.Product.Store.Name,
                                CurrencyCode = e.Product.Country != null ? e.Product.Country.Iso4 : null,
                                CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
                                Type = e.Product.Type,
                                ProductMediaFiles = e.Product.ProductMediaFiles
                                    .Where(pmf => pmf.MediaFile.IsActive)
                                    .Select(pmf => new MediaFileDetailsResponse
                                    {
                                        Uid = pmf.MediaFile.Uid,
                                        Url = pmf.MediaFile.Url,
                                        FileType = pmf.MediaFile.MediaFileType.ToString()
                                    }).ToList(),
                                ProductVariants = e.Product.ProductVariant
                                    .Select(pv => new ProductVariantResponse
                                    {
                                        VariantName = pv.VariantName,
                                        VariantOptions = pv.ProductVariantOptions.Select(opt => opt.Value).ToList()
                                    }).ToList(),
                                Profile = new ProfileBaseResponse
                                {
                                    Uid = e.Product.User.Profile.Uid,
                                    UserId = e.Product.User.Id,
                                    ImageUrl = e.Product.User.Profile.ImageUrl,
                                    //IsStore = false,
                                    FullName = e.Product.User.FirstName,
                                    FirstName = e.Product.User.FirstName,
                                    LastName = e.Product.User.LastName,
                                    Username = e.Product.User.UserName,
                                    DisplayName = e.Product.User.DisplayName,
                                    UserType = e.Product.User.Profile.UserType,
                                    FollowedByMe = cUser != null && e.Product.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id)
                                }
                            }
                        }).ToList(),
                        Profile = pms.Post.Store == null ? new ProfileBaseResponse()
                        {
                            Username = pms.Post.User.UserName,
                            ImageUrl = pms.Post.User.Profile.ImageUrl,
                        } : null,
                        //Store = pms.Post.Store == null
                        //    ? null
                        //    : new StoreBaseResponse()
                        //    {
                        //        Uid = pms.Post.Store.Uid,
                        //        Name = pms.Post.Store.Name,
                        //        UniqueName = pms.Post.Store.UniqueName,
                        //        CurrencyCode = pms.Post.Store.Currency.Code,
                        //        ImageUrl = pms.Post.Store.ImageUrl
                        //    },
                        CreatedAt = pms.CreatedAt,
                        ImageWidth = pms.Post.ImageWidth,
                        ImageHeight = pms.Post.ImageHeight,
                        VideoWidth = pms.Post.VideoWidth,
                        VideoHeight = pms.Post.VideoHeight,
                        PostType = PostTypeEnum.MyStyle,
                        IsMyStyle = true,
                        //PostedByStore = pms.Post.Store != null,
                        PostProfileMentions = pms.Post.PostProfileMentions.Select(e => new TaggedUserResponse
                        {
                            ProfileUid = e.Profile.Uid,
                            Username = e.Profile.User.UserName,
                            FirstName = e.Profile.User.FirstName,
                            ProfileImageUrl = e.Profile.ImageUrl,
                            FollowedByMe = cUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id),
                            UserType = e.Profile.UserType
                        }).ToList(),
                        PostHashtags = pms.Post.PostHashtags.Select(e => e.Hashtag.Value).ToList(),
                    });

                var list = await PagedList<PostResponse>.ToPagedListAsync(queryMapped, queryParams.PageNumber,
                    queryParams.PageSize);

                var res = _mapper.Map<PagingResponse<PostResponse>>(list);
                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}