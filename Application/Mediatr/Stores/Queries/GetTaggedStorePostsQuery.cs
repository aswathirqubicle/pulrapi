using System;
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
using Core.Application.Models.Stores;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Queries;

public class GetTaggedStorePostsQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
{
    [Required]
    public string StoreUid { get; set; }
}

public class GetTaggedStorePostsQueryHandler : IRequestHandler<GetTaggedStorePostsQuery, PagingResponse<PostResponse>>
{
    private readonly ILogger<GetTaggedStorePostsQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _dbContext;

    public GetTaggedStorePostsQueryHandler(
        ILogger<GetTaggedStorePostsQueryHandler> logger,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<PagingResponse<PostResponse>> Handle(GetTaggedStorePostsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var store = await _dbContext.Stores.Include(s => s.Currency).SingleOrDefaultAsync(s => s.Uid == request.StoreUid && s.IsActive, cancellationToken);

            if (store is null)
                throw new NotFoundException("Store not found");

            var currentUser = await _currentUserService.GetUserAsync();

            var postsQuery = _dbContext.PostProductTags
                .Where(ppt => ppt.Product.StoreId == store.Id)
                .Select(ppt => ppt.Post);

            // Filter out posts from blocked users
            if (currentUser != null)
            {
                var blockedProfileIds = await _dbContext.UserBlocks
                    .Where(ub => ub.BlockerProfileId == currentUser.Profile.Uid)
                    .Select(ub => ub.BlockedProfileId)
                    .ToListAsync(cancellationToken);

                postsQuery = postsQuery.Where(p => !blockedProfileIds.Contains(p.User.Profile.Uid));
            }

            // Filter out reported posts
            var reportedPostIds = await _dbContext.Reports
                .Where(r => r.ReportType == ReportTypeEnum.Post)
                .Select(r => r.EntityUid)
                .ToListAsync(cancellationToken);

            postsQuery = postsQuery.Where(p => !reportedPostIds.Contains(p.Uid));

            var postsMapped = postsQuery.Select(p => new PostResponse
            {
                Uid = p.Uid,
                //StoreUid = request.StoreUid,
                ProfileUid = null,
                Text = p.Text,
                ImageWidth = p.ImageWidth,
                ImageHeight = p.ImageHeight,
                VideoWidth = p.VideoWidth,
                VideoHeight = p.VideoHeight,
                MediaFile = _mapper.Map<MediaFileDetailsResponse>(p.MediaFile),
                LikesCount = p.PostLikes.Count(),
                LikedByMe = currentUser != null && p.PostLikes.Any(pl => pl.LikedById == currentUser.Profile.Id),
                TaggedProductUids = p.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => ppt.Product.Uid),
                CreatedAt = p.CreatedAt,
                //PostedByStore = true,
                PostProfileMentions = p.PostProfileMentions.Select(e => new TaggedUserResponse
                {
                    ProfileUid = e.Profile.Uid,
                    Username = e.Profile.User.UserName,
                    FirstName = e.Profile.User.FirstName,
                    ProfileImageUrl = e.Profile.ImageUrl,
                    FollowedByMe = currentUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentUser.Profile.Id),
                    UserType = e.Profile.UserType
                }).ToList(),
                //PostStoreMentions = p.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                BookmarkedByMe = currentUser != null && p.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci.BookmarkCollection.IsActive),
                BookmarksCount = p.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
                MyStylesCount = p.PostMyStyles.Count,
                //Store = p.Store != null
                //    ? new StoreBaseResponse()
                //    {
                //        Uid = p.Store.Uid,
                //        Name = p.Store.Name,
                //        ImageUrl = p.Store.ImageUrl,
                //        UniqueName = p.Store.UniqueName,
                //        CurrencyCode = p.Store.Currency.Code,
                //        FollowedByMe = currentUser != null && p.Store.StoreFollowers.Any(sf => sf.FollowerId == currentUser.Profile.Id),
                //    }
                //    : null,
                PostProductTags = p.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse()
                {
                    Product = new ProductPublicResponse()
                    {
                        Uid = e.Product.Uid,
                        Name = e.Product.Name,
                        Price = e.Product.MinPrice,
                        CurrencyCode = store.Currency.Code,
                        StoreName = e.Product.Store.Name,
                        CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
                        ProductMediaFiles = e.Product.ProductMediaFiles
                            .Where(pmf => pmf.MediaFile.IsActive)
                            .Select(pmf => new MediaFileDetailsResponse
                            {
                                Uid = pmf.MediaFile.Uid,
                                Url = pmf.MediaFile.Url,
                                FileType = pmf.MediaFile.MediaFileType.ToString()
                            }).ToList()
                    },
                    PositionLeftPercent = e.PositionLeftPercent,
                    PositionTopPercent = e.PositionTopPercent,
                    LocationX = e.LocationX,
                    LocationY = e.LocationY
                }).ToList(),
                Profile = null,
                //CommentsCount = p.Comments.Count,
                PostType = PostTypeEnum.TaggedOn
            });


            var list = await PagedList<PostResponse>.ToPagedListAsync(postsMapped, request.PageNumber,
                request.PageSize);

            var pagedPosts = _mapper.Map<PagingResponse<PostResponse>>(list);

            pagedPosts.ItemIds = pagedPosts.Items.Select(item => item.Uid).ToList();
            return pagedPosts;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting posts where the store is tagged");
            throw;
        }
    }
}