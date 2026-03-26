using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class ProfilesTopFollowedQuery : IRequest<List<ProfileResponse>>
    {
        [Range(1, 20)]
        public int Count { get; set; }

        public List<string> ProfileUidsToSkip { get; set; } = new List<string>();
    }

    public class ProfilesTopFollowedQueryHandler : IRequestHandler<ProfilesTopFollowedQuery, List<ProfileResponse>>
    {
        private readonly ILogger<ProfilesTopFollowedQueryHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ProfilesTopFollowedQueryHandler(ILogger<ProfilesTopFollowedQueryHandler> logger,  UserManager<User> userManager, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }
        public async Task<List<ProfileResponse>> Handle(ProfilesTopFollowedQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<Profile> queryableProfiles = _dbContext.Profiles;
                if (cUser?.Profile != null)
                {
                    queryableProfiles = queryableProfiles.Where(
                        profile => profile.IsActive == true && profile.Uid != cUser.Profile.Uid &&
                        // we skip profiles that user is already following:
                        profile.ProfileFollowers.Where(p => p.FollowerId == cUser.Profile.Id).Any() == false);
                }

                if(request.ProfileUidsToSkip.Count() > 0)
                {
                    queryableProfiles = queryableProfiles.Where(p => !request.ProfileUidsToSkip.Contains(p.Uid));
                }

                var topFollowedList = await queryableProfiles
                    .Where(p => p.IsActive == true && !p.User.IsSuspended)
                    .OrderByDescending(p => p.ProfileFollowers.Count()).Take(request.Count).Select(p => new ProfileResponse()
                    {
                        About = p.About,
                        FullName = p.User.FirstName,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        UserType = p.UserType,
                        Followers = p.ProfileFollowers.Count(),
                        Following = p.ProfileFollowings.Count(),
                        ImageUrl = p.ImageUrl,
                        UserId = p.UserId,
                        Uid = p.Uid,
                        Username = p.User.UserName,
                    }).ToListAsync(cancellationToken);

                if (topFollowedList.Any() && cUser != null)
                {
                    List<string> topFollowedListUids = topFollowedList.Select(p => p.Uid).ToList();

                    var myFollows = await _dbContext.ProfileFollowers
                        .Where(pf => pf.FollowerId == cUser.Profile.Id && topFollowedListUids.Contains(pf.Profile.Uid))
                        .Select(pf => pf.Profile.Uid).ToListAsync(cancellationToken);

                    foreach (var item in topFollowedList)
                    {
                        item.FollowedByMe = myFollows.Contains(item.Uid);
                        item.IsInfluencer = await _userManager.IsInRoleAsync(new User() { Id = item.UserId }, PulrRoles.Influencer);
                    }
                }

                return topFollowedList;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
