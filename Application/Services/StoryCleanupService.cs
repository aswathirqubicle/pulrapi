using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using System.Threading;

namespace Core.Application.Services
{
    public interface IStoryCleanupService
    {
        Task DeleteExpiredStoriesAsync();
    }

    public class StoryCleanupService : IStoryCleanupService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<StoryCleanupService> _logger;

        public StoryCleanupService(
            IApplicationDbContext dbContext,
            ILogger<StoryCleanupService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task DeleteExpiredStoriesAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of expired stories");

                var now = DateTime.UtcNow;

                // Find stories that have expired (regardless of IsActive status)
                var expiredStories = await _dbContext.Stories
                    .Where(s => s.StoryExpiresIn <= now)
                    .Include(s => s.StoryLikes)
                    .Include(s => s.StoryHashTags)
                    .Include(s => s.StoryProductTags)
                    .Include(s => s.StoryProfileMentions)
                    .Include(s => s.StorySeens)
                    .Include(s => s.MediaFile)
                    .ToListAsync();

                if (!expiredStories.Any())
                {
                    _logger.LogInformation("No expired stories found for deletion");
                    return;
                }

                _logger.LogInformation("Found {Count} expired stories to delete", expiredStories.Count);

                // Hard delete expired stories and all related data
                _dbContext.Stories.RemoveRange(expiredStories);

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation("Successfully deleted {Count} expired stories", expiredStories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting expired stories");
                throw;
            }
        }
    }
}
