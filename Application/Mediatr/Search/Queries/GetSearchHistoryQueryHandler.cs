using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetSearchHistoryQueryHandler : IRequestHandler<GetSearchHistoryQuery, List<SearchHistoryResponseDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetSearchHistoryQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<List<SearchHistoryResponseDto>> Handle(GetSearchHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserId();

            var history = await _dbContext.SearchHistories
                .Where(h => h.User.Id == userId && h.IsActive)
                .OrderByDescending(h => h.CreatedAt)
                .ThenByDescending(h => h.SearchCount)
                .Select(h => new SearchHistoryResponseDto
                {
                    Uid = h.Uid,
                    Term = h.Term,
                    Type = h.Type,
                    SearchCount = h.SearchCount,
                    UpdatedAt = h.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // For profile type searches, fetch and include profile details
            var profileSearches = history.Where(h => h.Type == SearchHistoryType.Profile).ToList();
            if (profileSearches.Any())
            {
                var usernames = profileSearches.Select(h => h.Term).ToList();
                var profiles = await _dbContext.Users
                    .Where(u => usernames.Contains(u.UserName))
                    .Select(u => new ProfileDto
                    {
                        Uid = u.Profile.Uid,
                        Username = u.UserName,
                        FullName = u.FirstName,
                        FollowersCount = u.Profile.ProfileFollowers.Count,
                        ImageUrl = u.Profile.ImageUrl ?? string.Empty
                    })
                    .ToListAsync(cancellationToken);

                foreach (var search in profileSearches)
                {
                    search.Profile = profiles.FirstOrDefault(p => p.Username == search.Term);
                }
            }

            // For hashtag type searches, fetch and include post count
            var hashtagSearches = history.Where(h => h.Type == SearchHistoryType.Hashtag).ToList();
            if (hashtagSearches.Any())
            {
                // Use the hashtag terms directly since they're stored with # in the database
                var hashtags = hashtagSearches.Select(h => h.Term).ToList();
                var hashtagCounts = await _dbContext.Hashtags
                    .Where(h => hashtags.Contains(h.Value))
                    .Select(h => new { Value = h.Value, PostCount = h.PostHashtags.Count })
                    .ToListAsync(cancellationToken);

                foreach (var search in hashtagSearches)
                {
                    var hashtagInfo = hashtagCounts.FirstOrDefault(h => h.Value == search.Term);
                    if (hashtagInfo != null)
                    {
                        search.HashtagInfo = new HashtagInfoDto
                        {
                            Value = hashtagInfo.Value,
                            Count = hashtagInfo.PostCount
                        };
                    }
                }
            }

            return history;
        }
    }
} 