using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Constants;
using Core.Application.Models;
using Microsoft.Extensions.Configuration;

namespace Core.Application.Services
{
    public interface IPostPurgeService
    {
        Task PurgeExpiredDeletedPostsAsync();
    }

    public class PostPurgeService : IPostPurgeService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<PostPurgeService> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;

        public PostPurgeService(
            IApplicationDbContext dbContext,
            ILogger<PostPurgeService> logger,
            IFileUploadService fileUploadService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _configuration = configuration;
        }

        public async Task PurgeExpiredDeletedPostsAsync()
        {
            try
            {
                _logger.LogInformation("Starting purge of expired deleted posts (30+ days old)");

                // Calculate cutoff date (30 days ago)
                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // Find posts that are soft deleted and older than 30 days
                var expiredPosts = await _dbContext.Posts
                    .Where(p => !p.IsActive && p.DeletedAt.HasValue && p.DeletedAt.Value < cutoffDate)
                    .Include(p => p.MediaFile)
                    .Include(p => p.Comments)
                    .Include(p => p.PostLikes)
                    .Include(p => p.PostHashtags)
                    .Include(p => p.PostProfileMentions)
                    .Include(p => p.PostStoreMentions)
                    .Include(p => p.PostProductTags)
                    .Include(p => p.PostClicks)
                    .Include(p => p.PostMyStyles)
                    .Include(p => p.Reports)
                    .ToListAsync();

                if (!expiredPosts.Any())
                {
                    _logger.LogInformation("No expired deleted posts found for purging");
                    return;
                }

                _logger.LogInformation("Found {Count} expired deleted posts to purge", expiredPosts.Count);

                var purgedCount = 0;
                var awsFilesDeleted = 0;

                foreach (var post in expiredPosts)
                {
                    try
                    {
                        // Delete AWS media files first
                        if (post.MediaFile != null && !string.IsNullOrEmpty(post.MediaFile.Url))
                        {
                            try
                            {
                                var fileConfig = new FileUploadConfigDto()
                                {
                                    OldFileName = post.MediaFile.Url.Substring(post.MediaFile.Url.LastIndexOf("/") + 1),
                                    BucketName = _configuration[AwsLocationNames.S3UploadBucket],
                                    FolderPath = _configuration[AwsLocationNames.PublicUploadFolder],
                                };

                                await _fileUploadService.Delete(fileConfig);
                                awsFilesDeleted++;
                                _logger.LogDebug("Successfully deleted MediaFile {MediaFileUid} from AWS bucket", post.MediaFile.Uid);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete MediaFile {MediaFileUid} from AWS bucket, continuing with purge", post.MediaFile.Uid);
                            }
                        }

                        // Delete ALL related entities that reference this post
                        // Comments (including replies) - access shadow property PostId
                        var comments = await _dbContext.Comments
                            .Where(c => EF.Property<int?>(c, "PostId") == post.Id)
                            .ToListAsync();
                        _dbContext.Comments.RemoveRange(comments);

                        // Comment likes
                        var commentLikes = await _dbContext.CommentLikes
                            .Where(cl => comments.Select(c => c.Id).Contains(cl.CommentId))
                            .ToListAsync();
                        _dbContext.CommentLikes.RemoveRange(commentLikes);

                        // Post likes
                        var postLikes = await _dbContext.PostLikes
                            .Where(pl => pl.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostLikes.RemoveRange(postLikes);

                        // Post hashtags
                        var postHashtags = await _dbContext.PostHashtags
                            .Where(ph => ph.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostHashtags.RemoveRange(postHashtags);

                        // Post profile mentions
                        var postProfileMentions = await _dbContext.PostProfileMentions
                            .Where(ppm => ppm.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostProfileMentions.RemoveRange(postProfileMentions);

                        // Post store mentions
                        var postStoreMentions = await _dbContext.PostStoreMentions
                            .Where(psm => psm.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostStoreMentions.RemoveRange(postStoreMentions);

                        // Post product tags
                        var postProductTags = await _dbContext.PostProductTags
                            .Where(ppt => ppt.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostProductTags.RemoveRange(postProductTags);

                        // Bookmarks
                        // Note: Bookmarks are now handled through collections, no need to remove them separately

                        // Post clicks
                        var postClicks = await _dbContext.PostClicks
                            .Where(pc => pc.Post.Id == post.Id)
                            .ToListAsync();
                        _dbContext.PostClicks.RemoveRange(postClicks);

                        // Post my styles
                        var postMyStyles = await _dbContext.PostMyStyles
                            .Where(pms => pms.PostId == post.Id)
                            .ToListAsync();
                        _dbContext.PostMyStyles.RemoveRange(postMyStyles);

                        // Reports
                        var reports = await _dbContext.Reports
                            .Where(r => r.EntityUid == post.Uid)
                            .ToListAsync();
                        _dbContext.Reports.RemoveRange(reports);

                        // Stories that reference this post
                        var stories = await _dbContext.Stories
                            .Where(s => s.SharedPostId == post.Id)
                            .ToListAsync();
                        _dbContext.Stories.RemoveRange(stories);

                        // Delete the media file record
                        if (post.MediaFile != null)
                        {
                            _dbContext.MediaFiles.Remove(post.MediaFile);
                        }

                        // Finally delete the post itself
                        _dbContext.Posts.Remove(post);

                        purgedCount++;
                        _logger.LogDebug("Purged post {PostUid} and all related data", post.Uid);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error purging post {PostUid}, skipping", post.Uid);
                    }
                }

                // Save all changes
                await _dbContext.SaveChangesAsync(cancellationToken: default);

                _logger.LogInformation("Successfully purged {PurgedCount} expired deleted posts and {AwsFilesDeleted} AWS media files", 
                    purgedCount, awsFilesDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during post purge process");
                throw;
            }
        }
    }
}
