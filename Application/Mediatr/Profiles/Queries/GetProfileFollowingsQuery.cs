using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileFollowingsQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        [Required]
        public string ProfileUid { get; set; }
    }

    public class GetProfileFollowingsQueryHandler : IRequestHandler<GetProfileFollowingsQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProfileFollowingsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetProfileFollowingsQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetProfileFollowingsQueryHandler> logger,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetProfileFollowingsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var currentProfileId = cUser?.Profile?.Id;
                var currentProfileUid = cUser?.Profile?.Uid;
                var dateTimeNow = DateTime.UtcNow;

                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .Include(p => p.ProfileSettings)
                    .FirstOrDefaultAsync(p => p.Uid == request.ProfileUid, cancellationToken);

                if (profile == null)
                {
                    profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .Include(p => p.ProfileSettings)
                        .FirstOrDefaultAsync(p => p.User.UserName.ToLower() == request.ProfileUid.ToLower(), cancellationToken);
                }

                if (profile == null)
                {
                    throw new BadRequestException("Profile doesn't exist");
                }

                bool isProfilePublic = profile.ProfileSettings == null || profile.ProfileSettings.IsProfilePublic;

                bool currentUserFollowsProfile = currentProfileId.HasValue &&
                    await _dbContext.ProfileFollowers.AnyAsync(
                        pf => pf.ProfileId == profile.Id && pf.FollowerId == currentProfileId.Value, cancellationToken);
                bool profileFollowsCurrentUser = currentProfileId.HasValue &&
                    await _dbContext.ProfileFollowers.AnyAsync(
                        pf => pf.ProfileId == currentProfileId.Value && pf.FollowerId == profile.Id, cancellationToken);

                if (!isProfilePublic)
                {
                    bool isOwner = currentProfileId.HasValue && profile.Id == currentProfileId.Value;
                    if (!isOwner && !currentUserFollowsProfile)
                        throw new ForbiddenException("This profile is private.");
                }

                var followingsQuery = _dbContext.ProfileFollowers
                    .Where(pf => pf.FollowerId == profile.Id && pf.Profile.IsActive)
                    .Select(pf => new ProfileDetailsResponse
                    {
                        Uid          = pf.Profile.Uid,
                        FullName     = pf.Profile.User.FirstName,
                        FirstName    = pf.Profile.User.FirstName,
                        LastName     = pf.Profile.User.LastName,
                        Username     = pf.Profile.User.UserName,
                        ImageUrl     = pf.Profile.ImageUrl,
                        Followers    = pf.Profile.ProfileFollowers.Count(),
                        Following    = pf.Profile.ProfileFollowings.Count(),
                        PostsCount   = pf.Profile.User.Posts.Count(post => post.IsActive),
                        About        = pf.Profile.About,
                        Location     = pf.Profile.Location,
                        IsProfilePublic = pf.Profile.ProfileSettings == null || pf.Profile.ProfileSettings.IsProfilePublic,
                        WebsiteUrl   = pf.Profile.ProfileSocialMedia.WebsiteUrl,
                        InstagramUrl = pf.Profile.ProfileSocialMedia.InstagramUrl,
                        FacebookUrl  = pf.Profile.ProfileSocialMedia.FacebookUrl,
                        TwitterUrl   = pf.Profile.ProfileSocialMedia.TwitterUrl,
                        TikTokUrl    = pf.Profile.ProfileSocialMedia.TikTokUrl,
                        ActiveStoriesCount = cUser != null
                            ? pf.Profile.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                            : pf.Profile.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow),
                        StoriesSeenCount = pf.Profile.User.Stories
                            .Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow)
                            .SelectMany(story => story.StorySeens)
                            .Select(seen => seen.SeenById)
                            .Distinct()
                            .Count(),
                        UnseenStoriesCount = cUser != null
                            ? pf.Profile.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                            : pf.Profile.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow),
                        FollowedByMe = currentProfileId.HasValue &&
                            pf.Profile.ProfileFollowers.Any(pf2 => pf2.FollowerId == currentProfileId.Value),
                        IsFollowingMe = currentProfileId.HasValue &&
                            pf.Profile.ProfileFollowings.Any(pf2 => pf2.ProfileId == currentProfileId.Value),
                        FollowRequestSent = currentProfileUid != null && _dbContext.FollowRequests
                            .Any(fr => fr.RequesterProfileId == currentProfileUid
                                    && fr.TargetProfileId == pf.Profile.Uid
                                    && fr.IsActive),
                        CanFollowBack = currentProfileId.HasValue &&
                            pf.Profile.ProfileFollowings.Any(pf2 => pf2.ProfileId == currentProfileId.Value) &&
                            !pf.Profile.ProfileFollowers.Any(pf2 => pf2.FollowerId == currentProfileId.Value),
                        Stores = pf.Profile.User.Stores.Select(s => new StoreDetailsResponse
                        {
                            Followers  = s.StoreFollowers.Count(),
                            Name       = s.Name,
                            ImageUrl   = s.ImageUrl,
                            Uid        = s.Uid,
                            UniqueName = s.UniqueName
                        }).ToList()
                    });

                var list = await PagedList<ProfileDetailsResponse>.ToPagedListAsync(followingsQuery, request.PageNumber, request.PageSize);
                var result = _mapper.Map<PagingResponse<ProfileDetailsResponse>>(list);
                result.FollowedByMe = currentUserFollowsProfile;
                result.IsFollowingMe = profileFollowsCurrentUser;
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
