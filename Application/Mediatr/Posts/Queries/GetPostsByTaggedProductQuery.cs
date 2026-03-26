using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Posts.Queries
{
	public class GetPostsByTaggedProductQuery : PagingParamsRequest, IRequest<PagingResponse<PostDetailsResponse>>
	{
		public string ProductUid { get; set; }
		public string ExcludePostUid { get; set; }
		public string CurrencyCode { get; set; }
	}

	public class GetPostsByTaggedProductQueryHandler : IRequestHandler<GetPostsByTaggedProductQuery, PagingResponse<PostDetailsResponse>>
	{
		private readonly ILogger<GetPostsByTaggedProductQueryHandler> _logger;
		private readonly IApplicationDbContext _dbContext;
		private readonly IMapper _mapper;
		private readonly IExchangeRateService _exchangeRateService;
		private readonly ICurrentUserService _currentUserService;

		public GetPostsByTaggedProductQueryHandler(
			ILogger<GetPostsByTaggedProductQueryHandler> logger,
			IApplicationDbContext dbContext,
			IMapper mapper,
			IExchangeRateService exchangeRateService,
			ICurrentUserService currentUserService)
		{
			_logger = logger;
			_dbContext = dbContext;
			_mapper = mapper;
			_exchangeRateService = exchangeRateService;
			_currentUserService = currentUserService;
		}

		public async Task<PagingResponse<PostDetailsResponse>> Handle(GetPostsByTaggedProductQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Uid == request.ProductUid && p.IsActive, cancellationToken);
				if (product == null)
				{
					throw new Exception("Product not found");
				}

				var currentUser = await _currentUserService.GetUserAsync();

				var postsQuery = _dbContext.Posts
					.Include(p => p.User).ThenInclude(u => u.Profile)
						.ThenInclude(pr => pr.ProfileFollowers)
					.Include(p => p.PostProductTags)
						.ThenInclude(pt => pt.Product)
							.ThenInclude(p => p.User)
								.ThenInclude(u => u.Profile)
									.ThenInclude(pr => pr.ProfileFollowers)
					.Include(p => p.PostProductTags)
						.ThenInclude(pt => pt.Product)
							.ThenInclude(p => p.ProductMediaFiles)
								.ThenInclude(pmf => pmf.MediaFile)
					.Include(p => p.PostProductTags)
						.ThenInclude(pt => pt.Product)
							.ThenInclude(p => p.Store)
								.ThenInclude(s => s.Currency)
					.Include(p => p.PostProductTags)
						.ThenInclude(pt => pt.Product)
							.ThenInclude(p => p.ProductVariant)
								.ThenInclude(pv => pv.ProductVariantOptions)
					.Include(p => p.MediaFile)
					.Include(p => p.PostLikes)
					.Include(p => p.PostMyStyles)
					.Include(p => p.Comments)
					.Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
					.Include(p => p.PostProfileMentions).ThenInclude(ppm => ppm.Profile).ThenInclude(pr => pr.User)
					.Include(p => p.PostStoreMentions).ThenInclude(psm => psm.Store)
					.Include(p => p.Store)
					.Where(p => p.IsActive
						&& p.PostProductTags.Any(ppt => ppt.Product.Uid == request.ProductUid)
						&& (string.IsNullOrEmpty(request.ExcludePostUid) || p.Uid != request.ExcludePostUid));

				// Filter out posts from blocked users
				if (currentUser != null && currentUser.Profile != null)
				{
					var blockedProfileIds = await _dbContext.UserBlocks
						.Where(ub => ub.BlockerProfileId == currentUser.Profile.Uid)
						.Select(ub => ub.BlockedProfileId)
						.ToListAsync(cancellationToken);

					postsQuery = postsQuery.Where(p => p.User.Profile != null && !blockedProfileIds.Contains(p.User.Profile.Uid));
				}

				// Filter out reported posts
				var reportedPostIds = await _dbContext.Reports
					.Where(r => r.ReportType == Core.Domain.Enums.ReportTypeEnum.Post && r.IsActive)
					.Select(r => r.EntityUid)
					.ToListAsync(cancellationToken);

				postsQuery = postsQuery.Where(p => !reportedPostIds.Contains(p.Uid));

				// Order by the owner's followers count desc
				postsQuery = postsQuery
					.OrderByDescending(p => p.User.Profile.ProfileFollowers.Count());

				// Currency handling like GetPostQuery
				var currency = request.CurrencyCode != null
					? await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Code == request.CurrencyCode, cancellationToken)
					: null;

				var postResponsesQuery = postsQuery.Select(post => new PostDetailsResponse
				{
					Uid = post.Uid,
					Text = post.Text,
					ImgDescription = post.ImgDescription,
					ThumbnailUrl = string.IsNullOrEmpty(post.ThumbnailUrl) ? (post.MediaFile != null ? (post.MediaFile.OriginalUrl ?? post.MediaFile.Url) : null) : post.ThumbnailUrl,
					ImageWidth = post.ImageWidth,
					ImageHeight = post.ImageHeight,
					VideoWidth = post.VideoWidth,
					VideoHeight = post.VideoHeight,
					CreatedAt = post.CreatedAt,
					LikesCount = post.PostLikes.Count(),
					CommentsCount = post.Comments.Count(c => c.IsActive),
					BookmarksCount = post.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
					BookmarkedByMe = currentUser != null && post.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci.BookmarkCollection.IsActive),
					MyStylesCount = post.PostMyStyles.Count,
					LikedByMe = currentUser != null ? post.PostLikes.Any(pl => pl.LikedById == currentUser.Profile.Id) : false,
					MediaFile = _mapper.Map<MediaFileDetailsResponse>(post.MediaFile),
					PostHashtags = post.PostHashtags.Where(e => e.Hashtag != null).Select(e => e.Hashtag.Value).ToList(),
					PostProfileMentions = post.PostProfileMentions.Where(e => e.Profile != null && e.Profile.User != null).Select(e => new TaggedUserResponse
					{
                        ProfileUid = e.Profile.Uid,
						Username = e.Profile.User.UserName,
						FirstName = e.Profile.User.FirstName,
						ProfileImageUrl = e.Profile.ImageUrl,
						FollowedByMe = currentUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentUser.Profile.Id),
						UserType = e.Profile.UserType
					}).ToList(),
					PostStoreMentions = post.PostStoreMentions.Where(e => e.Store != null).Select(e => e.Store.UniqueName).ToList(),
					PostType = Core.Domain.Enums.PostTypeEnum.Feed,
					ProfileUid = post.Store == null ? post.User.Profile.Uid : null,
					Profile = new Core.Application.Models.Profiles.ProfileDetailsResponse
					{
						Uid = post.User.Profile.Uid,
						Username = post.User.UserName,
						FullName = post.User.FirstName,
						FirstName = post.User.FirstName,
						DisplayName = post.User.DisplayName,
						LastName = post.User.LastName,
						ImageUrl = post.User.Profile.ImageUrl,
						FollowedByMe = currentUser != null ? post.User.Profile.ProfileFollowers.Any(pf => pf.FollowerId == currentUser.Profile.Id) : false,
					},
					StoreUid = post.Store != null ? post.Store.Uid : null,
					PostedByStore = post.Store != null,
					PostProductTags = post.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse
					{
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
							Price = currency != null && e.Product.Store != null && e.Product.Store.Currency != null ? _exchangeRateService.GetCurrencyExchangeRates(e.Product.Store.Currency.Code, currency.Code, e.Product.MinPrice, null) : e.Product.MinPrice,
							CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
							CurrencyCode = currency != null ? currency.Code : (e.Product.Store != null && e.Product.Store.Currency != null ? e.Product.Store.Currency.Code : null),
							StoreName = e.Product.Store != null ? e.Product.Store.Name : null,
							ProductMediaFiles = e.Product.ProductMediaFiles
								.Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive)
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
							ProductVariants = e.Product.ProductVariant
								.Where(pv => pv.ProductVariantOptions != null)
								.Select(pv => new ProductVariantResponse
								{
									VariantName = pv.VariantName,
									VariantOptions = pv.ProductVariantOptions.Select(opt => opt.Value).ToList()
								}).ToList()
						},
						PositionLeftPercent = e.PositionLeftPercent,
						PositionTopPercent = e.PositionTopPercent,
						LocationX = e.LocationX,
						LocationY = e.LocationY
					}).ToList()
				});

				var paged = await PagedList<PostDetailsResponse>.ToPagedListAsync(postResponsesQuery, request.PageNumber, request.PageSize);
				var response = _mapper.Map<PagingResponse<PostDetailsResponse>>(paged);
				return response;
			}
			catch (Exception e)
			{
				_logger.LogError(e, e.Message);
				throw;
			}
		}
	}
}
