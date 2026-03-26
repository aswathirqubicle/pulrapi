using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Users.Queries
{
    public class GetBlockedUsersQuery : IRequest<List<BlockedUserDto>>
    {
    }

    public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, List<BlockedUserDto>>
    {
        private readonly ILogger<GetBlockedUsersQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetBlockedUsersQueryHandler(
            ILogger<GetBlockedUsersQueryHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext,
            IMapper mapper)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<BlockedUserDto>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUserService.GetUserAsync();
            if (currentUser == null)
            {
                throw new Exception("User is not authenticated");
            }

            var blockedUsers = await _dbContext.UserBlocks
                .Include(ub => ub.BlockedProfile)
                .ThenInclude(p => p.User)
                .Where(ub => ub.BlockerProfile.UserId == currentUser.Id && ub.IsActive)
                .Select(ub => new BlockedUserDto
                {
                    Uid = ub.BlockedProfile.User.Profile.Uid,
                    Username = ub.BlockedProfile.User.UserName,
                    FullName = ub.BlockedProfile.User.FirstName,
                    ImageUrl = ub.BlockedProfile.ImageUrl,
                    BlockedAt = ub.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return blockedUsers;
        }
    }
} 