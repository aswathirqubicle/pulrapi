using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models.Profiles;
using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetMostFollowedProfilesQuery : IRequest<List<MostFollowedProfileDto>>
    {
        public int Limit { get; set; }
    }

    public class GetMostFollowedProfilesQueryHandler : IRequestHandler<GetMostFollowedProfilesQuery, List<MostFollowedProfileDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetMostFollowedProfilesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<MostFollowedProfileDto>> Handle(GetMostFollowedProfilesQuery request, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var currentProfileId = currentUser?.Profile?.Id;

            var mostFollowed = await _dbContext.ProfileFollowers
                .GroupBy(f => f.ProfileId)
                .Select(g => new { ProfileId = g.Key, Followers = g.Count() })
                .OrderByDescending(x => x.Followers)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var profileIds = mostFollowed.Select(x => x.ProfileId).ToList();

            var profiles = await _dbContext.Profiles
                .Include(p => p.User)
                .Include(p => p.ProfileSettings)
                .Where(p => profileIds.Contains(p.Id) &&
                            (p.ProfileSettings == null || p.ProfileSettings.IsProfilePublic))
                .ToListAsync(cancellationToken);

            var result = new List<MostFollowedProfileDto>();
            foreach (var item in mostFollowed)
            {
                var profile = profiles.FirstOrDefault(p => p.Id == item.ProfileId);
                if (profile == null) continue;
                var user = profile.User;
                var followingCount = await _dbContext.ProfileFollowers.CountAsync(f => f.FollowerId == profile.Id, cancellationToken);
                var postsCount = await _dbContext.Posts.CountAsync(post => post.User.Id == user.Id && post.IsActive, cancellationToken);
                var followedByMe = currentProfileId != null && await _dbContext.ProfileFollowers.AnyAsync(f => f.ProfileId == profile.Id && f.FollowerId == currentProfileId, cancellationToken);
                result.Add(new MostFollowedProfileDto
                {
                    Uid = profile.Uid,
                    ImageUrl = profile.ImageUrl,
                    FullName = user.FirstName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserType = profile.UserType,
                    Followers = item.Followers,
                    Following = followingCount,
                    FollowedByMe = followedByMe,
                    UserId = user.Id,
                    Username = user.UserName,
                    PostsCount = postsCount,
                    About = profile.About,
                    Stores = null, // You can populate this if needed
                    PostedTimeAgo = user.CreatedAt
                });
            }
            return result;
        }
    }
}
