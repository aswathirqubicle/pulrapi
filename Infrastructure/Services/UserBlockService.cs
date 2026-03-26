using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly ILogger<UserBlockService> _logger;
        private readonly IApplicationDbContext _dbContext;

        public UserBlockService(
            ILogger<UserBlockService> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task HandleUserBlock(string blockerProfileUid, string blockedProfileUid, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Deactivate any existing follows between the users
                var followings = await _dbContext.ProfileFollowers
                    .Where(pf => 
                        (pf.Profile.Uid == blockerProfileUid && pf.Follower.Uid == blockedProfileUid) ||
                        (pf.Profile.Uid == blockedProfileUid && pf.Follower.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var following in followings)
                {
                    following.IsActive = false;
                    following.UpdatedAt = DateTime.UtcNow;
                }

                // Note: Bookmarks are now handled through collections, no need to deactivate them separately

                // 3. Deactivate any existing likes
                var likes = await _dbContext.PostLikes
                    .Where(pl => 
                        (pl.LikedBy.Uid == blockerProfileUid && pl.Post.User.Profile.Uid == blockedProfileUid) ||
                        (pl.LikedBy.Uid == blockedProfileUid && pl.Post.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var like in likes)
                {
                    like.IsActive = false;
                    like.UpdatedAt = DateTime.UtcNow;
                }

                // 4. Deactivate any existing comments
                var comments = await _dbContext.Comments
                    .Where(c => 
                        (c.CommentedBy.Uid == blockerProfileUid && c.Post.User.Profile.Uid == blockedProfileUid) ||
                        (c.CommentedBy.Uid == blockedProfileUid && c.Post.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var comment in comments)
                {
                    comment.IsActive = false;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                // 5. Deactivate any existing story likes
                var storyLikes = await _dbContext.StoryLikes
                    .Where(sl => 
                        (sl.LikedBy.Uid == blockerProfileUid && sl.Story.User.Profile.Uid == blockedProfileUid) ||
                        (sl.LikedBy.Uid == blockedProfileUid && sl.Story.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var storyLike in storyLikes)
                {
                    storyLike.IsActive = false;
                    storyLike.UpdatedAt = DateTime.UtcNow;
                }

                // 6. Deactivate any existing story views
                var storyViews = await _dbContext.StorySeens
                    .Where(sv => 
                        (sv.SeenBy.Uid == blockerProfileUid && sv.Story.User.Profile.Uid == blockedProfileUid) ||
                        (sv.SeenBy.Uid == blockedProfileUid && sv.Story.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var storyView in storyViews)
                {
                    storyView.IsActive = false;
                    storyView.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user block effects");
                throw;
            }
        }

        public async Task HandleUserUnblock(string blockerProfileUid, string blockedProfileUid, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Reactivate any existing follows between the users
                var followings = await _dbContext.ProfileFollowers
                    .Where(pf => 
                        (pf.Profile.Uid == blockerProfileUid && pf.Follower.Uid == blockedProfileUid) ||
                        (pf.Profile.Uid == blockedProfileUid && pf.Follower.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var following in followings)
                {
                    following.IsActive = true;
                    following.UpdatedAt = DateTime.UtcNow;
                }

                // Note: Bookmarks are now handled through collections, no need to reactivate them separately

                // 3. Reactivate any existing likes
                var likes = await _dbContext.PostLikes
                    .Where(pl => 
                        (pl.LikedBy.Uid == blockerProfileUid && pl.Post.User.Profile.Uid == blockedProfileUid) ||
                        (pl.LikedBy.Uid == blockedProfileUid && pl.Post.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var like in likes)
                {
                    like.IsActive = true;
                    like.UpdatedAt = DateTime.UtcNow;
                }

                // 4. Reactivate any existing comments
                var comments = await _dbContext.Comments
                    .Where(c => 
                        (c.CommentedBy.Uid == blockerProfileUid && c.Post.User.Profile.Uid == blockedProfileUid) ||
                        (c.CommentedBy.Uid == blockedProfileUid && c.Post.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var comment in comments)
                {
                    comment.IsActive = true;
                    comment.UpdatedAt = DateTime.UtcNow;
                }

                // 5. Reactivate any existing story likes
                var storyLikes = await _dbContext.StoryLikes
                    .Where(sl => 
                        (sl.LikedBy.Uid == blockerProfileUid && sl.Story.User.Profile.Uid == blockedProfileUid) ||
                        (sl.LikedBy.Uid == blockedProfileUid && sl.Story.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var storyLike in storyLikes)
                {
                    storyLike.IsActive = true;
                    storyLike.UpdatedAt = DateTime.UtcNow;
                }

                // 6. Reactivate any existing story views
                var storyViews = await _dbContext.StorySeens
                    .Where(sv => 
                        (sv.SeenBy.Uid == blockerProfileUid && sv.Story.User.Profile.Uid == blockedProfileUid) ||
                        (sv.SeenBy.Uid == blockedProfileUid && sv.Story.User.Profile.Uid == blockerProfileUid))
                    .ToListAsync(cancellationToken);

                foreach (var storyView in storyViews)
                {
                    storyView.IsActive = true;
                    storyView.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user unblock effects");
                throw;
            }
        }
    }
} 