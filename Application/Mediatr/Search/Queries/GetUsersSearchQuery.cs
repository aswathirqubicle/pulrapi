using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetUsersSearchQuery : IRequest<PaginatedResultDto<UserSearchResultDto>>
    {
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int? PageSize { get; set; }
    }

    public class GetUsersSearchQueryHandler : IRequestHandler<GetUsersSearchQuery, PaginatedResultDto<UserSearchResultDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private const int DefaultPageSize = 5;
        private readonly ICurrentUserService _currentUserService;

        public GetUsersSearchQueryHandler(
            IApplicationDbContext dbContext, 
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedResultDto<UserSearchResultDto>> Handle(GetUsersSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //await _mediator.Publish(new CreateSearchHistoryEntryNotification
                //{
                //    Term = request.SearchTerm
                //}, cancellationToken);

                // Validate and set default values for pagination
                var page = request.Page <= 0 ? 1 : request.Page;
                var pageSize = request.PageSize ?? DefaultPageSize;
                if (pageSize <= 0) pageSize = DefaultPageSize;

                var searchTerm = request.SearchTerm?.ToLower() ?? string.Empty;

                var cUser = await _currentUserService.GetUserAsync();
                var myProfileUid = cUser?.Profile?.Uid;
                var blockedProfileIds = await _dbContext.UserBlocks
                    .Where(ub => (ub.BlockerProfileId == myProfileUid || ub.BlockedProfileId == myProfileUid) && ub.IsActive)
                    .Select(ub => ub.BlockerProfileId == myProfileUid ? ub.BlockedProfileId : ub.BlockerProfileId)
                    .ToListAsync(cancellationToken);

                // Create the base query
                var baseQuery = _dbContext.Users
                    .Include(u => u.Profile)
                    .Include(u => u.Profile.ProfileFollowers)
                    .Where(u => u.IsVerified && !u.IsSuspended && 
                           u.Profile != null &&
                           !blockedProfileIds.Contains(u.Profile.Uid) &&
                           (u.FirstName.ToLower().Contains(searchTerm) ||
                           u.UserName.ToLower().Contains(searchTerm) ||
                            u.LastName.ToLower().Contains(searchTerm)));

                // Get total count
                var totalCount = await baseQuery.CountAsync(cancellationToken);

                // Get paginated results
                var items = await baseQuery
                    //.OrderByDescending(u => u.CreatedAt)
                    //.Select(u => new UserSearchResultDto
                    .OrderByDescending(u =>
                        u.UserName.ToLower().StartsWith(searchTerm) ? 2 :
                        (u.FirstName.ToLower().StartsWith(searchTerm) || u.LastName.ToLower().StartsWith(searchTerm)) ? 1 : 0)
                    .ThenBy(u => u.UserName.ToLower().IndexOf(searchTerm)) // Prioritize closer matches in username
                    .ThenBy(u => u.FirstName.ToLower().IndexOf(searchTerm)) // Then in first name
                    .ThenByDescending(u => u.CreatedAt)
                    .Select(u => new UserSearchResultDto
                    {
                        Uid = u.Profile.Uid,
                        Username = u.UserName ?? string.Empty,
                        DisplayName = u.DisplayName ?? string.Empty,
                        FullName = u.FirstName ?? string.Empty,
                        ImageUrl = u.Profile.ImageUrl ?? string.Empty,
                        FollowersCount = u.Profile.ProfileFollowers.Count()

                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return PaginatedResultDto<UserSearchResultDto>.Create(
                    page,
                    pageSize,
                    totalCount,
                    items);
            }
            catch (Exception ex)
            {
                throw new Exception("Error searching users", ex);
            }
        }
    }
}
