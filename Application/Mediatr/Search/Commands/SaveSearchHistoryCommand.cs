using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Search.Commands
{
    public class SaveSearchHistoryCommand : IRequest<SearchHistoryResponseDto>
    {
        public string Term { get; set; }
        public SearchHistoryType Type { get; set; }
    }

    public class SaveSearchHistoryCommandHandler : IRequestHandler<SaveSearchHistoryCommand, SearchHistoryResponseDto>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public SaveSearchHistoryCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<SearchHistoryResponseDto> Handle(SaveSearchHistoryCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserId();

            // Check if this search history already exists
            var existingHistory = await _dbContext.SearchHistories
                .FirstOrDefaultAsync(h => 
                    h.User.Id == userId && 
                    h.Term == request.Term && 
                    h.Type == request.Type, 
                    cancellationToken);

            if (existingHistory != null)
            {
                // Update the existing entry
                //if(request.Type == SearchHistoryType.Hashtag)
                //{
                //    request.Term = request.Term.StartsWith('#') ? request.Term : $"#{request.Term}";
                //}
                existingHistory.SearchCount++;
                existingHistory.IsActive = true; // Ensure the entry is active
                existingHistory.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create a new entry
                var user = await _dbContext.Users.FindAsync(userId);
                if (request.Type == SearchHistoryType.Hashtag)
                {
                    request.Term = request.Term.StartsWith('#') ? request.Term : $"#{request.Term}";
                }
                existingHistory = new Core.Domain.Entities.SearchHistory
                {
                    User = user,
                    Term = request.Term,
                    Type = request.Type,
                    SearchCount = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.SearchHistories.Add(existingHistory);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Create response
            var response = new SearchHistoryResponseDto
            {
                Uid = existingHistory.Uid,
                Term = existingHistory.Term,
                Type = existingHistory.Type,
                SearchCount = existingHistory.SearchCount,
                UpdatedAt = existingHistory.UpdatedAt
            };

            // If type is Profile, include profile details
            if (request.Type == SearchHistoryType.Profile)
            {
                var profile = await _dbContext.Users
                    .Where(u => u.UserName == request.Term)
                    .Select(u => new ProfileDto
                    {
                        Uid = u.Profile.Uid,
                        Username = u.UserName,
                        FullName = u.FirstName,
                        FollowersCount = u.Profile.ProfileFollowers.Count,
                        ImageUrl = u.Profile.ImageUrl ?? string.Empty
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                response.Profile = profile;
            }
            if (request.Type == SearchHistoryType.Hashtag)
            {
                var hashtagInfo = await _dbContext.Hashtags
                    .Where(h => h.Value == request.Term)
                    .Select(h => new HashtagInfoDto
                    {
                        Value = h.Value,
                        Count = h.PostHashtags.Count
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                response.HashtagInfo = hashtagInfo;
            }

            return response;
        }
    }
} 