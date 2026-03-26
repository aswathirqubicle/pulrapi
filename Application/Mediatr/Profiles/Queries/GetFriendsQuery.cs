using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Profiles.Queries;

public class GetFriendsQuery : IRequest<List<ProfileResponse>>
{
    public int Count { get; set; } = 10;
    public string Search { get; set; }
}

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, List<ProfileResponse>>
{
    private readonly ILogger<GetFriendsQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public GetFriendsQueryHandler(
        ILogger<GetFriendsQueryHandler> logger,
        IApplicationDbContext dbContext,
        IProfileService profileService,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _profileService = profileService;
        _currentUserService = currentUserService;
    }

    public async Task<List<ProfileResponse>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            var myFollowsQuery = _dbContext.ProfileFollowers
                .AsSplitQuery()
                .Include(pf => pf.Profile).ThenInclude(p => p.User)
                .Where(pf => !pf.Profile.User.IsSuspended && pf.FollowerId == currentUser.Profile.Id);

            if (!String.IsNullOrWhiteSpace(request.Search))
            {
                myFollowsQuery = myFollowsQuery.Where(p => p.Profile.User.FirstName.ToLower().Contains(request.Search.Trim().ToLower())
                                                        //|| p.Profile.User.LastName.ToLower().Contains(request.Search.Trim().ToLower())
                                                        || p.Profile.User.UserName.ToLower().Contains(request.Search.Trim().ToLower())
                                                        || p.Profile.User.Email.ToLower().Contains(request.Search.Trim().ToLower())
                );
            }

            var profiles = await _profileService.MapProfileResponseList(myFollowsQuery.OrderByDescending(pf => pf.CreatedAt).Select(p => p.Profile).Take(request.Count), cancellationToken);

            return profiles;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting friendly profiles");
            throw;
        }
    }
}