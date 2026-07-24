using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Posts.Queries
{
    public class GetSimilarPostsByTaggedProductsQuery : PagingParamsRequest, IRequest<PagingResponse<PostDetailsResponse>>
    {
        [Required]
        public string PostUid { get; set; }

        public string CurrencyCode { get; set; }

        [Range(1, 10, ErrorMessage = "MaxProductMatches must be between 1 and 10")]
        public int MaxProductMatches { get; set; } = 3;

        public bool IncludeBoughtSimilar { get; set; } = false;
        public bool IncludeWishlist { get; set; } = false;
    }

    public class GetSimilarPostsByTaggedProductsQueryHandler : IRequestHandler<GetSimilarPostsByTaggedProductsQuery, PagingResponse<PostDetailsResponse>>
    {
        private readonly ILogger<GetSimilarPostsByTaggedProductsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;
        private readonly IExchangeRateService _exchangeRateService;

        public GetSimilarPostsByTaggedProductsQueryHandler(
            ILogger<GetSimilarPostsByTaggedProductsQueryHandler> logger,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IApplicationDbContext dbContext,
            IExchangeRateService exchangeRateService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _dbContext = dbContext;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<PagingResponse<PostDetailsResponse>> Handle(GetSimilarPostsByTaggedProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Get current post info and tagged products (Minimal fetch)
                var currentPost = await _dbContext.Posts
                    .AsNoTracking()
                    .Where(p => p.Uid == request.PostUid && p.IsActive)
                    .Select(p => new {
                        ProductUids = p.PostProductTags
                            .Where(ppt => ppt.Product != null)
                            .Select(ppt => ppt.Product.Uid)
                            .ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentPost == null) throw new NotFoundException("Post not found");

                var currentUser = await _currentUserService.GetUserAsync();
                var currentProfileId = currentUser?.Profile?.Id;
                var currentProfileUid = currentUser?.Profile?.Uid;

                // 2a. Expand source product set for bought-similar and bag-similar
                var boughtSimilarProductUids = new HashSet<string>();
                var bagSimilarProductUids    = new HashSet<string>();

                if (currentProfileId != null)
                {
                    if (request.IncludeBoughtSimilar)
                    {
                        var boughtData = await _dbContext.OrderProductAffiliates
                            .AsNoTracking()
                            .Where(opa => opa.Order.IsActive && opa.Order.ProfileId == currentProfileId)
                            .Select(opa => new { opa.ProductId, opa.Product.WhatIsIt, opa.Product.Brand, opa.Product.Name })
                            .Distinct()
                            .ToListAsync(cancellationToken);

                        var exactBoughtIds    = boughtData.Select(x => x.ProductId).ToHashSet();
                        var boughtWhatIsItSet = boughtData.Where(x => !string.IsNullOrEmpty(x.WhatIsIt)).Select(x => x.WhatIsIt).ToHashSet();
                        var boughtBrandSet    = boughtData.Where(x => !string.IsNullOrEmpty(x.Brand)).Select(x => x.Brand).ToHashSet();
                        var boughtNames       = boughtData.Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => x.Name).ToList();

                        if (boughtWhatIsItSet.Any() || boughtBrandSet.Any() || boughtNames.Any())
                        {
                            boughtSimilarProductUids = (await _dbContext.Products
                                .AsNoTracking()
                                .Where(p => p.IsActive
                                         && !exactBoughtIds.Contains(p.Id)
                                         && (boughtWhatIsItSet.Contains(p.WhatIsIt)
                                             || boughtBrandSet.Contains(p.Brand)
                                             || boughtNames.Any(n => p.Name.Contains(n))))
                                .Select(p => p.Uid)
                                .ToListAsync(cancellationToken))
                                .ToHashSet();
                        }
                    }

                    if (request.IncludeWishlist)
                    {
                        var bagProductUids = (await _dbContext.UserBagProducts
                            .AsNoTracking()
                            .Where(ubp => ubp.UserId == currentUser.Id)
                            .Select(ubp => ubp.BagProduct.Uid)
                            .Distinct()
                            .ToListAsync(cancellationToken))
                            .ToHashSet();

                        if (bagProductUids.Any())
                        {
                            var bagDetails = await _dbContext.Products
                                .AsNoTracking()
                                .Where(p => bagProductUids.Contains(p.Uid))
                                .Select(p => new { p.WhatIsIt, p.Brand, p.Name })
                                .ToListAsync(cancellationToken);

                            var bagWhatIsItSet = bagDetails.Where(x => !string.IsNullOrEmpty(x.WhatIsIt)).Select(x => x.WhatIsIt).ToHashSet();
                            var bagBrandSet    = bagDetails.Where(x => !string.IsNullOrEmpty(x.Brand)).Select(x => x.Brand).ToHashSet();
                            var bagNames       = bagDetails.Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => x.Name).ToList();

                            if (bagWhatIsItSet.Any() || bagBrandSet.Any() || bagNames.Any())
                            {
                                bagSimilarProductUids = (await _dbContext.Products
                                    .AsNoTracking()
                                    .Where(p => p.IsActive
                                             && (bagWhatIsItSet.Contains(p.WhatIsIt)
                                                 || bagBrandSet.Contains(p.Brand)
                                                 || bagNames.Any(n => p.Name.Contains(n))))
                                    .Select(p => p.Uid)
                                    .ToListAsync(cancellationToken))
                                    .ToHashSet();
                            }
                        }
                    }
                }

                var expandedProductUids = new HashSet<string>(currentPost.ProductUids);
                expandedProductUids.UnionWith(boughtSimilarProductUids);
                expandedProductUids.UnionWith(bagSimilarProductUids);

                if (!expandedProductUids.Any())
                    return new PagingResponse<PostDetailsResponse> { Items = new List<PostDetailsResponse>(), CurrentPage = request.PageNumber, PageSize = request.PageSize };

                // 2. Fetch exchange rates (Batched)
                List<ExchangeRate> exchangeRates = null;
                bool doExchangeRate = false;
                if (!string.IsNullOrEmpty(request.CurrencyCode))
                {
                    var storeCurrencyCodes = await _dbContext.Products
                        .AsNoTracking()
                        .Where(p => currentPost.ProductUids.Contains(p.Uid) && p.Store != null && p.Store.Currency != null)
                        .Select(p => p.Store.Currency.Code)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    var currencyCodes = new List<string> { request.CurrencyCode };
                    currencyCodes.AddRange(storeCurrencyCodes);
                    exchangeRates = await _exchangeRateService.GetExchangeRates(currencyCodes);
                    doExchangeRate = currencyCodes != null && exchangeRates != null;
                }

                // 3. Find Similar Posts (IDs and Scores only)
                var similarPostsBaseQuery = _dbContext.Posts
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.Uid != request.PostUid)
                    .Where(p => p.PostProductTags.Any(ppt => ppt.Product != null && expandedProductUids.Contains(ppt.Product.Uid)));

                // Filter blocks and reports (DB side)
                if (!string.IsNullOrEmpty(currentProfileUid))
                {
                    similarPostsBaseQuery = similarPostsBaseQuery.Where(p => !_dbContext.UserBlocks
                        .Any(ub => ub.IsActive && 
                                   ((ub.BlockerProfileId == currentProfileUid && ub.BlockedProfileId == p.User.Profile.Uid) || 
                                    (ub.BlockedProfileId == currentProfileUid && ub.BlockerProfileId == p.User.Profile.Uid))));
                }

                similarPostsBaseQuery = similarPostsBaseQuery.Where(p => !_dbContext.Reports
                    .Any(r => r.ReportType == ReportTypeEnum.Post && r.IsActive && r.EntityUid == p.Uid));

                // Filter out posts from private profiles that the current user doesn't follow
                if (currentProfileId.HasValue)
                {
                    similarPostsBaseQuery = similarPostsBaseQuery.Where(p =>
                        p.User.Profile.ProfileSettings == null ||
                        p.User.Profile.ProfileSettings.IsProfilePublic ||
                        p.User.Profile.Id == currentProfileId.Value ||
                        _dbContext.ProfileFollowers.Any(pf =>
                            pf.ProfileId == p.User.Profile.Id && pf.FollowerId == currentProfileId.Value));
                }
                else
                {
                    similarPostsBaseQuery = similarPostsBaseQuery.Where(p =>
                        p.User.Profile.ProfileSettings == null ||
                        p.User.Profile.ProfileSettings.IsProfilePublic);
                }

                var totalCount = await similarPostsBaseQuery.CountAsync(cancellationToken);
                if (totalCount == 0) return new PagingResponse<PostDetailsResponse> { Items = new List<PostDetailsResponse>(), CurrentPage = request.PageNumber, PageSize = request.PageSize };

                var pagedPostsInfo = await similarPostsBaseQuery
                    .Select(p => new
                    {
                        Uid = p.Uid,
                        ProductMatchCount  = p.PostProductTags.Count(ppt => ppt.Product != null && currentPost.ProductUids.Contains(ppt.Product.Uid)),
                        BoughtSimilarCount = p.PostProductTags.Count(ppt => ppt.Product != null && boughtSimilarProductUids.Contains(ppt.Product.Uid)),
                        BagSimilarCount    = p.PostProductTags.Count(ppt => ppt.Product != null && bagSimilarProductUids.Contains(ppt.Product.Uid)),
                        FollowerCount      = p.User.Profile.ProfileFollowers.Count,
                        CreatedAt          = p.CreatedAt
                    })
                    .OrderByDescending(x =>
                        (x.BagSimilarCount    * 300) +
                        (x.BoughtSimilarCount * 200) +
                        (x.ProductMatchCount  * 100) +
                        x.FollowerCount)
                    .ThenByDescending(x => x.FollowerCount)
                    .ThenByDescending(x => x.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                var pagedUids = pagedPostsInfo.Select(x => x.Uid).ToList();

                // 4. Fetch FULL DATA via PROJECTION (The main performance boost)
                var pagedPostsData = await _dbContext.Posts
                    .AsNoTracking()
                    .Where(p => pagedUids.Contains(p.Uid))
                    .Select(p => new {
                        p.Uid, p.Text, p.ImgDescription, p.CreatedAt, p.ThumbnailUrl,
                        p.ImageWidth, p.ImageHeight, p.VideoWidth, p.VideoHeight,
                        LikesCount = p.PostLikes.Count,
                        CommentsCount = p.Comments.Count(c => c.IsActive),
                        BookmarksCount = p.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
                        BookmarkedByMe = currentProfileId != null && p.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == currentProfileId && bci.BookmarkCollection.IsActive),
                        MyStylesCount = p.PostMyStyles.Count,
                        LikedByMe = currentProfileId != null && p.PostLikes.Any(pl => pl.LikedById == currentProfileId),
                        MediaFile = p.MediaFile == null ? null : new { p.MediaFile.Uid, p.MediaFile.Url, p.MediaFile.OriginalUrl, p.MediaFile.MediaFileType },
                        Hashtags = p.PostHashtags.Where(ph => ph.Hashtag != null).Select(ph => ph.Hashtag.Value).ToList(),
                        Mentions = p.PostProfileMentions.Where(pm => pm.Profile != null && pm.Profile.User != null).Select(pm => new {
                            pm.Profile.Uid, 
                            pm.Profile.User.UserName, 
                            pm.Profile.User.FirstName, 
                            pm.Profile.ImageUrl,
                            pm.Profile.UserType,
                            FollowedByMe = currentProfileId != null && pm.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentProfileId)
                        }).ToList(),
                        StoreMentions = p.PostStoreMentions.Where(psm => psm.Store != null).Select(psm => psm.Store.UniqueName).ToList(),
                        Profile = new {
                            Uid = p.Store == null ? p.User.Profile.Uid : "",
                            Username = p.User.UserName,
                            FirstName = p.User.FirstName,
                            DisplayName = p.User.DisplayName,
                            LastName = p.User.LastName,
                            ImageUrl = p.User.Profile.ImageUrl,
                            FollowedByMe = currentProfileId != null && p.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentProfileId)
                        },
                        Store = p.Store == null ? null : new { p.Store.Uid, p.Store.Name, p.Store.UniqueName },
                        ProductTags = p.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => new {
                            ppt.PositionLeftPercent, ppt.PositionTopPercent, ppt.LocationX, ppt.LocationY,
                            Product = new {
                                ppt.Product.Uid, ppt.Product.Name, ppt.Product.WhatIsIt, ppt.Product.ProductDetail, ppt.Product.Brand, ppt.Product.MinPrice, ppt.Product.MaxPrice, ppt.Product.ProductUrl,
                                CountryIso2 = ppt.Product.Country == null ? null : ppt.Product.Country.Iso2,
                                CurrencyCode = ppt.Product.Store.Currency.Code,
                                StoreName = ppt.Product.Store.Name,
                                MediaFiles = ppt.Product.ProductMediaFiles.Where(pmf => pmf.MediaFile.IsActive).Select(pmf => new { pmf.MediaFile.Uid, pmf.MediaFile.Url, pmf.MediaFile.MediaFileType }).ToList(),
                                Variants = ppt.Product.ProductVariant.Select(pv => new { pv.VariantName, Options = pv.ProductVariantOptions.Select(o => o.Value).ToList() }).ToList(),
                                ProductProfile = new {
                                    ppt.Product.User.Profile.Uid, ppt.Product.User.UserName, ppt.Product.User.FirstName, ppt.Product.User.LastName, ppt.Product.User.DisplayName, ppt.Product.User.Profile.ImageUrl,
                                    FollowedByMe = currentProfileId != null && ppt.Product.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentProfileId)
                                }
                            }
                        }).ToList()
                    })
                    .ToListAsync(cancellationToken);

                // 5. Transform results to Response DTOs
                var postsResponse = pagedUids
                    .Select(uid => pagedPostsData.FirstOrDefault(d => d.Uid == uid))
                    .Where(d => d != null)
                    .Select(d => new PostDetailsResponse
                    {
                        Uid = d.Uid, Text = d.Text, ImgDescription = d.ImgDescription, CreatedAt = d.CreatedAt,
                        ThumbnailUrl = string.IsNullOrEmpty(d.ThumbnailUrl) ? (d.MediaFile != null ? (d.MediaFile.OriginalUrl ?? d.MediaFile.Url) : null) : d.ThumbnailUrl,
                        ImageWidth = d.ImageWidth, ImageHeight = d.ImageHeight, VideoWidth = d.VideoWidth, VideoHeight = d.VideoHeight,
                        LikesCount = d.LikesCount, CommentsCount = d.CommentsCount, BookmarksCount = d.BookmarksCount, BookmarkedByMe = d.BookmarkedByMe, MyStylesCount = d.MyStylesCount, LikedByMe = d.LikedByMe,
                        MediaFile = d.MediaFile == null ? null : new MediaFileDetailsResponse { Uid = d.MediaFile.Uid, Url = d.MediaFile.Url, FileType = d.MediaFile.MediaFileType.ToString() },
                        PostHashtags = d.Hashtags,
                        PostProfileMentions = d.Mentions.Select(m => new TaggedUserResponse { ProfileUid = m.Uid, Username = m.UserName, FirstName = m.FirstName, ProfileImageUrl = m.ImageUrl, UserType = m.UserType, FollowedByMe = m.FollowedByMe }).ToList(),
                        PostStoreMentions = d.StoreMentions,
                        PostType = PostTypeEnum.Feed,
                        ProfileUid = d.Uid, 
                        Profile = new ProfileDetailsResponse { Uid = d.Profile.Uid, Username = d.Profile.Username, FullName = d.Profile.FirstName, FirstName = d.Profile.FirstName, DisplayName = d.Profile.DisplayName, LastName = d.Profile.LastName, ImageUrl = d.Profile.ImageUrl, FollowedByMe = d.Profile.FollowedByMe },
                        StoreUid = d.Store?.Uid, PostedByStore = d.Store != null,
                        PostProductTags = d.ProductTags.Select(pt => new PostProductTagResponse {
                            PositionLeftPercent = pt.PositionLeftPercent, PositionTopPercent = pt.PositionTopPercent, LocationX = pt.LocationX, LocationY = pt.LocationY,
                            Product = new ProductPublicResponse {
                                Uid = pt.Product.Uid, Name = pt.Product.Name, WhatIsIt = pt.Product.WhatIsIt, ProductDetail = pt.Product.ProductDetail, Brand = pt.Product.Brand, MinPrice = pt.Product.MinPrice, MaxPrice = pt.Product.MaxPrice, ProductUrl = pt.Product.ProductUrl,
                                Price = doExchangeRate ? _exchangeRateService.GetCurrencyExchangeRates(pt.Product.CurrencyCode, request.CurrencyCode, pt.Product.MinPrice, exchangeRates) : pt.Product.MinPrice,
                                CountryCode = pt.Product.CountryIso2,
                                CurrencyCode = pt.Product.CurrencyCode,
                                StoreName = pt.Product.StoreName,
                                ProductMediaFiles = pt.Product.MediaFiles.Select(mf => new MediaFileDetailsResponse { Uid = mf.Uid, Url = mf.Url, FileType = mf.MediaFileType.ToString() }).ToList(),
                                ProductVariants = pt.Product.Variants.Select(v => new ProductVariantResponse { VariantName = v.VariantName, VariantOptions = v.Options }).ToList(),
                                Profile = new ProfileBaseResponse { Uid = pt.Product.ProductProfile.Uid, Username = pt.Product.ProductProfile.UserName, FullName = pt.Product.ProductProfile.FirstName, FirstName = pt.Product.ProductProfile.FirstName, LastName = pt.Product.ProductProfile.LastName, DisplayName = pt.Product.ProductProfile.DisplayName, ImageUrl = pt.Product.ProductProfile.ImageUrl, FollowedByMe = pt.Product.ProductProfile.FollowedByMe }
                            }
                        }).ToList(),
                        TaggedProductUids = d.ProductTags.Select(pt => pt.Product.Uid).ToList()
                    }).ToList();

                return new PagingResponse<PostDetailsResponse> { Items = postsResponse, CurrentPage = request.PageNumber, PageSize = request.PageSize, TotalCount = totalCount, TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize) };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting similar posts by tagged products");
                throw;
            }
        }
    }
}
