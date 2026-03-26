using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Models;
using Core.Domain.Enums;
using System.Linq;
using System.Diagnostics;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class DeletePostCommand : IRequest<Unit>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Unit>
    {
        private readonly ILogger<DeletePostCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;

        public DeletePostCommandHandler(
            ILogger<DeletePostCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext,
            IConfiguration configuration,
            IFileUploadService fileUploadService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("DeletePost started for PostUid={PostUid}", request.Uid);

                // ============================================
                // STEP 1: Get Current User
                // ============================================
                var currentUser = await _currentUserService.GetUserAsync(true);

                // ============================================
                // STEP 2: Find Post with Minimal Data
                // ============================================
                // ✅ Only load what we need - no unnecessary includes
                var postInfo = await _dbContext.Posts
                    .Where(p => p.Uid == request.Uid && p.User == currentUser)
                    .Select(p => new
                    {
                        PostId = p.Id,
                        PostUid = p.Uid,
                        MediaFileId = p.MediaFile.Id,
                        MediaFileUid = p.MediaFile != null ? p.MediaFile.Uid : null,
                        MediaFileUrl = p.MediaFile != null ? p.MediaFile.Url : null,
                        HasMediaFile = p.MediaFile != null
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (postInfo == null)
                {
                    _logger.LogWarning(
                        "DeletePost failed: Post not found or unauthorized. PostUid={PostUid}, UserId={UserId}",
                        request.Uid,
                        currentUser.Id);

                    throw new BadRequestException(
                        $"Post with UID '{request.Uid}' not found or you do not have permission to delete it.");
                }

                _logger.LogInformation(
                    "DeletePost: Found post. PostUid={PostUid}, PostId={PostId}, HasMediaFile={HasMediaFile}",
                    postInfo.PostUid,
                    postInfo.PostId,
                    postInfo.HasMediaFile);

                // ============================================
                // STEP 3: Execute Deletion with Statistics
                // ============================================
                var now = DateTime.UtcNow;
                var stats = new DeletionStatistics();

                // ============================================
                // 3.1: Delete Comment Likes (Batch Operation)
                // ============================================
                // ✅ Use EF.Property for shadow property access
                var commentLikesDeleted = await _dbContext.CommentLikes
                    .Where(cl => EF.Property<int?>(cl.Comment, "PostId") == postInfo.PostId)
                    .ExecuteDeleteAsync(cancellationToken);

                stats.CommentLikesDeleted = commentLikesDeleted;

                _logger.LogDebug(
                    "DeletePost: Deleted {CommentLikeCount} comment likes. PostUid={PostUid}",
                    commentLikesDeleted,
                    request.Uid);

                // ============================================
                // 3.2: Delete Comments (Batch Operation)
                // ============================================
                var commentsDeleted = await _dbContext.Comments
                    .Where(c => EF.Property<int?>(c, "PostId") == postInfo.PostId)
                    .ExecuteDeleteAsync(cancellationToken);

                stats.CommentsDeleted = commentsDeleted;

                _logger.LogDebug(
                    "DeletePost: Deleted {CommentCount} comments. PostUid={PostUid}",
                    commentsDeleted,
                    request.Uid);

                // ============================================
                // 3.3: Delete Post Likes (Batch Operation)
                // ============================================
                var postLikesDeleted = await _dbContext.PostLikes
                    .Where(pl => pl.Post.Id == postInfo.PostId)
                    .ExecuteDeleteAsync(cancellationToken);

                stats.PostLikesDeleted = postLikesDeleted;

                _logger.LogDebug(
                    "DeletePost: Deleted {PostLikeCount} post likes. PostUid={PostUid}",
                    postLikesDeleted,
                    request.Uid);

                // ============================================
                // 3.4: Delete Post Profile Mentions (Batch Operation)
                // ============================================
                var profileMentionsDeleted = await _dbContext.PostProfileMentions
                    .Where(ppm => ppm.Post.Id == postInfo.PostId)
                    .ExecuteDeleteAsync(cancellationToken);

                stats.ProfileMentionsDeleted = profileMentionsDeleted;

                _logger.LogDebug(
                    "DeletePost: Deleted {MentionCount} profile mentions. PostUid={PostUid}",
                    profileMentionsDeleted,
                    request.Uid);

                // ============================================
                // 3.5: Delete Other Related Entities (Batch Operations)
                // ============================================
                await DeleteOtherRelatedEntitiesAsync(postInfo.PostId, request.Uid, stats, cancellationToken);

                // ============================================
                // 3.6: Soft Delete the Post (Batch Update)
                // ============================================
                var postUpdated = await _dbContext.Posts
                    .Where(p => p.Id == postInfo.PostId)
                    .ExecuteUpdateAsync(
                        p => p
                            .SetProperty(post => post.IsActive, false)
                            .SetProperty(post => post.DeletedAt, now)
                            .SetProperty(post => post.UpdatedAt, now),
                        cancellationToken);

                _logger.LogDebug("DeletePost: Soft deleted post. PostUid={PostUid}", request.Uid);

                // ============================================
                // 3.7: Soft Delete Related Stories (if needed)
                // ============================================
                // Uncomment if you want to soft delete stories
                // stats.StoriesDeleted = await _dbContext.Stories
                //     .Where(s => s.SharedPost.Id == postInfo.PostId && s.IsActive)
                //     .ExecuteUpdateAsync(
                //         s => s
                //             .SetProperty(story => story.IsActive, false)
                //             .SetProperty(story => story.UpdatedAt, now),
                //         cancellationToken);

                // ============================================
                // 3.8: Soft Delete MediaFile (Batch Update)
                // ============================================
                // ✅ Only soft delete media file if it's NOT used by any product
                if (postInfo.HasMediaFile)
                {
                    // Check if this media file is referenced by any product
                    var isUsedByProduct = await _dbContext.ProductMediaFiles
                        .Where(pmf => pmf.MediaFileId == postInfo.MediaFileId)
                        .AnyAsync(cancellationToken);

                    if (!isUsedByProduct)
                    {
                        var mediaFilesUpdated = await _dbContext.MediaFiles
                            .Where(mf => mf.Id == postInfo.MediaFileId)
                            .ExecuteUpdateAsync(
                                mf => mf
                                    .SetProperty(m => m.IsActive, false)
                                    .SetProperty(m => m.UpdatedAt, now),
                                cancellationToken);

                        _logger.LogInformation(
                            "DeletePost: Soft deleted MediaFile. MediaFileUid={MediaFileUid}, PostUid={PostUid}",
                            postInfo.MediaFileUid,
                            request.Uid);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "DeletePost: MediaFile NOT deleted as it's used by a product. MediaFileUid={MediaFileUid}, PostUid={PostUid}",
                            postInfo.MediaFileUid,
                            request.Uid);
                    }
                }

                // ============================================
                // 3.9: Delete Notifications (Batch Operation)
                // ============================================
                var notificationsDeleted = await _dbContext.NotificationHistories
                    .Where(n => n.TargetId == request.Uid && n.TargetType == EntityTypeEnum.POST)
                    .ExecuteDeleteAsync(cancellationToken);

                stats.NotificationsDeleted = notificationsDeleted;

                _logger.LogDebug(
                    "DeletePost: Deleted {NotificationCount} notifications. PostUid={PostUid}",
                    notificationsDeleted,
                    request.Uid);

                // ============================================
                // STEP 4: Log Success with Statistics
                // ============================================
                stopwatch.Stop();

                // ============================================
                // STEP 5: Background File Deletion (Non-Blocking)
                // ============================================
                // ✅ Media files are NOT deleted immediately from AWS
                // They will be deleted after 30 days by PostPurgeService
                // This allows for recovery during the 30-day grace period
                // if (postInfo.HasMediaFile && !string.IsNullOrEmpty(postInfo.MediaFileUrl))
                // {
                //     // Fire and forget - don't wait for S3 deletion
                //     _ = Task.Run(async () =>
                //     {
                //         try
                //         {
                //             await DeleteMediaFileFromStorageAsync(
                //                 postInfo.MediaFileUrl,
                //                 postInfo.MediaFileUid);
                //         }
                //         catch (Exception ex)
                //         {
                //             _logger.LogWarning(ex,
                //                 "Background deletion of MediaFile from storage failed. " +
                //                 "MediaFileUid={MediaFileUid}, PostUid={PostUid}",
                //                 postInfo.MediaFileUid,
                //                 request.Uid);
                //         }
                //     }, CancellationToken.None);
                // }

                return Unit.Value;
            }
            catch (BadRequestException)
            {
                // Re-throw validation exceptions without logging as errors
                throw;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                _logger.LogError(e,
                    "DeletePost failed after {ElapsedMs}ms. PostUid={PostUid}, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    request.Uid,
                    e.Message);
                throw;
            }
        }

        /// <summary>
        /// Deletes other related entities that should cascade when a post is deleted.
        /// Uses batch operations for better performance.
        /// ✅ NOTE: Products are NOT deleted - only the PostProductTag association is removed
        /// </summary>
        private async Task DeleteOtherRelatedEntitiesAsync(
            int postId,
            string postUid,
            DeletionStatistics stats,
            CancellationToken cancellationToken)
        {
            // ✅ Delete post product tags (hard delete)
            // This removes the association between post and product
            // The Product itself is NOT deleted (protected by OnDelete(DeleteBehavior.Restrict))
            stats.ProductTagsDeleted = await _dbContext.PostProductTags
                .Where(ppt => ppt.PostId == postId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug(
                "DeletePost: Deleted {ProductTagCount} product tags. PostUid={PostUid}",
                stats.ProductTagsDeleted,
                postUid);

            // ✅ Delete post hashtags (hard delete)
            stats.HashtagsDeleted = await _dbContext.PostHashtags
                .Where(ph => ph.PostId == postId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug(
                "DeletePost: Deleted {HashtagCount} hashtags. PostUid={PostUid}",
                stats.HashtagsDeleted,
                postUid);

            // ✅ Delete bookmark collection items (hard delete)
            stats.BookmarksDeleted = await _dbContext.BookmarkCollectionItems
                .Where(bci => bci.PostId == postId)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogDebug(
                "DeletePost: Deleted {BookmarkCount} bookmarks. PostUid={PostUid}",
                stats.BookmarksDeleted,
                postUid);
        }

        /// <summary>
        /// Deletes media file from cloud storage (S3/Azure/etc).
        /// This runs in the background and doesn't block the API response.
        /// </summary>
        private async Task DeleteMediaFileFromStorageAsync(string mediaFileUrl, string mediaFileUid)
        {
            try
            {
                if (string.IsNullOrEmpty(mediaFileUrl))
                {
                    return;
                }

                var fileName = mediaFileUrl.Substring(mediaFileUrl.LastIndexOf("/") + 1);

                var fileConfig = new FileUploadConfigDto()
                {
                    OldFileName = fileName,
                    BucketName = _configuration[AwsLocationNames.S3UploadBucket],
                    FolderPath = _configuration[AwsLocationNames.PublicUploadFolder],
                };

                await _fileUploadService.Delete(fileConfig);

                _logger.LogInformation(
                    "Successfully deleted MediaFile from cloud storage. " +
                    "MediaFileUid={MediaFileUid}, FileName={FileName}",
                    mediaFileUid,
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to delete MediaFile from cloud storage. " +
                    "MediaFileUid={MediaFileUid}, Url={Url}. " +
                    "File will remain in storage but is marked as inactive in database.",
                    mediaFileUid,
                    mediaFileUrl);
            }
        }

        /// <summary>
        /// Statistics class to track deletion counts for logging.
        /// </summary>
        private class DeletionStatistics
        {
            public int CommentsDeleted { get; set; }
            public int CommentLikesDeleted { get; set; }
            public int PostLikesDeleted { get; set; }
            public int ProfileMentionsDeleted { get; set; }
            public int ProductTagsDeleted { get; set; }
            public int HashtagsDeleted { get; set; }
            public int StoreMentionsDeleted { get; set; }
            public int BookmarksDeleted { get; set; }
            public int MyStylesDeleted { get; set; }
            public int NotificationsDeleted { get; set; }
            public int StoriesDeleted { get; set; }
        }
    }
}