using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Stores;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetStoreByNameQuery : IRequest<StoreDetailsResponse>
    {
        [Required]
        public string UniqueName { get; set; }
    }

    public class GetStoreByNameQueryHandler : IRequestHandler<GetStoreByNameQuery, StoreDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GetStoreByNameQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetStoreByNameQueryHandler(
            IApplicationDbContext dbContext,
            IMapper mapper,
            ILogger<GetStoreByNameQueryHandler> logger,
            ICurrentUserService currentUserService
        )
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<StoreDetailsResponse> Handle(GetStoreByNameQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var dateTimeNow = DateTime.UtcNow;

                var storeRes = await _dbContext.Stores
                    .Where(p => p.UniqueName == request.UniqueName && !p.User.IsSuspended && p.IsActive == true)
                    .Select(s => new StoreDetailsResponse()
                    {
                        AffiliateId = (cUser != null ? s.UserId == cUser.Id : false) ? s.Affiliate.AffiliateId : null,
                        ImageUrl = s.ImageUrl,
                        BannerUrl = s.BannerUrl,
                        ProfileUid = s.User.Profile.Uid,
                        UniqueName = s.UniqueName,
                        FollowedByMe = cUser != null && cUser.Profile != null ? s.StoreFollowers.Any(s => s.FollowerId == cUser.Profile.Id) : false,
                        Name = s.Name,
                        LegalName = s.LegalName,
                        Description = s.Description,
                        Uid = s.Uid,
                        StoreEmail = s.IsEmailPublic || (cUser != null ? s.UserId == cUser.Id : false) ? s.StoreEmail : null,
                        IsEmailPublic = s.IsEmailPublic,
                        Followers = s.StoreFollowers.Count(),
                        ProductsCount = s.Products.Count(),
                        LikesCount = s.LikesCount,
                        PostsCount = s.Posts.Where(p => p.IsActive).Count(),
                        Location = s.Location,
                        PhoneNumber = s.PhoneNumber,
                        WebsiteUrl = s.StoreSocialMedia.WebsiteUrl,
                        InstagramUrl = s.StoreSocialMedia.InstagramUrl,
                        FacebookUrl = s.StoreSocialMedia.FacebookUrl,
                        TwitterUrl = s.StoreSocialMedia.TwitterUrl,
                        TikTokUrl = s.StoreSocialMedia.TikTokUrl,
                        IsMyStore = cUser != null ? s.UserId == cUser.Id : false,
                        CurrencyCode = s.Currency.Code,
                        CurrencyUid = s.Currency.Uid,
                        ActiveStoriesCount = cUser != null 
                            ? s.Stories.Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id)).Count() 
                            : s.Stories.Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow).Count(),
                        StoriesSeenCount = s.Stories
                            .Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow)
                            .SelectMany(story => story.StorySeens)
                            .Select(seen => seen.SeenById)
                            .Distinct()
                            .Count(),
                        UnseenStoriesCount = cUser != null 
                            ? s.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                            : s.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow)
                    }).SingleOrDefaultAsync(cancellationToken);

                var storeRatings = await _dbContext.StoreRatings.Where(sr => sr.Store.Uid == storeRes.Uid)
                    .ToListAsync(cancellationToken);

                storeRes.RatingAverage = storeRatings.Count() >= 10
                    ? storeRatings.Select(sr => sr.NumberOfStars).Average()
                    : 4;

                if (cUser != null)
                {
                    storeRes.FollowedBy = await _dbContext.StoreFollowers
                        .Where(pf => pf.FollowerId == cUser.Profile.Id || pf.Follower.Uid == storeRes.Uid)
                        .OrderByDescending(pf => pf.Store.StoreFollowers.Count)
                        .Select(pf => pf.Store.User.UserName)
                        .ToListAsync(cancellationToken);
                }

                return storeRes;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}