using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetPendingFollowRequestsQuery : IRequest<List<FollowRequestDto>>
    {
    }

    public class GetPendingFollowRequestsQueryHandler : IRequestHandler<GetPendingFollowRequestsQuery, List<FollowRequestDto>>
    {
        private readonly ILogger<GetPendingFollowRequestsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetPendingFollowRequestsQueryHandler(ILogger<GetPendingFollowRequestsQueryHandler> logger, 
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<FollowRequestDto>> Handle(GetPendingFollowRequestsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                var profileUid = user.Profile.Uid;

                var followRequests = await _dbContext.FollowRequests
                    .AsNoTracking()
                    .Where(fr => fr.TargetProfileId == profileUid && fr.IsActive)
                    .Select(fr => new FollowRequestDto
                    {
                        Uid = fr.Uid,
                        RequesterProfileUid = fr.RequesterProfileId,
                        RequesterName = _dbContext.Profiles
                            .Where(p => p.Uid == fr.RequesterProfileId)
                            .Select(p => p.User.UserName)
                            .FirstOrDefault(),
                        RequesterAvatar = _dbContext.Profiles
                            .Where(p => p.Uid == fr.RequesterProfileId)
                            .Select(p => p.ImageUrl)
                            .FirstOrDefault(),
                        RequestedAt = fr.RequestedAt
                    })
                    .ToListAsync(cancellationToken);

                return followRequests;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
