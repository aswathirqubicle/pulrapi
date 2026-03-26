using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class DeleteAllUserPostsCommand : IRequest<DeleteAllUserPostsResult>
    {
        [Required]
        public string UserIdentifier { get; set; }
    }

    public class DeleteAllUserPostsResult
    {
        public string UserIdentifier { get; set; }
        public int PostsDeleted { get; set; }
        public int AwsFilesDeleted { get; set; }
        public int AwsFilesFailed { get; set; }
        public int CommentLikesDeleted { get; set; }
        public int CommentsDeleted { get; set; }
        public int PostLikesDeleted { get; set; }
        public int RelatedEntitiesDeleted { get; set; }
        public long ElapsedMs { get; set; }
    }

    public class DeleteAllUserPostsCommandHandler : IRequestHandler<DeleteAllUserPostsCommand, DeleteAllUserPostsResult>
    {
        private readonly ILogger<DeleteAllUserPostsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;

        public DeleteAllUserPostsCommandHandler(
            ILogger<DeleteAllUserPostsCommandHandler> logger,
            IApplicationDbContext dbContext,
            IConfiguration configuration,
            IFileUploadService fileUploadService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        public async Task<DeleteAllUserPostsResult> Handle(
            DeleteAllUserPostsCommand request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new DeleteAllUserPostsResult { UserIdentifier = request.UserIdentifier };

            _logger.LogInformation(
                "[DevTool] DeleteAllUserPosts started. UserIdentifier={UserIdentifier}",
                request.UserIdentifier);

            // ============================================
            // STEP 1: Resolve User
            // ============================================
            var identifier = request.UserIdentifier.Trim().ToLowerInvariant();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.NormalizedEmail == identifier.ToUpperInvariant()
                      || u.NormalizedUserName == identifier.ToUpperInvariant(),
                    cancellationToken);

            if (user == null)
            {
                throw new NotFoundException($"User '{request.UserIdentifier}' not found.");
            }

            _logger.LogInformation(
                "[DevTool] Resolved user. UserId={UserId}, Email={Email}",
                user.Id, user.Email);

            // ============================================
            // STEP 2: Load Post Metadata (IDs + media URLs)
            // ============================================
            var posts = await _dbContext.Posts
                .Where(p => p.User.Id == user.Id)
                .Select(p => new PostMetadata
                {
                    PostId = p.Id,
                    PostUid = p.Uid,
                    MediaFileId = (int?)p.MediaFile.Id,
                    MediaFileUid = p.MediaFile != null ? p.MediaFile.Uid : null,
                    MediaFileUrl = p.MediaFile != null ? p.MediaFile.Url : null,
                })
                .ToListAsync(cancellationToken);

            if (!posts.Any())
            {
                _logger.LogInformation(
                    "[DevTool] No posts found for user. UserId={UserId}", user.Id);
                return result;
            }

            var postIds = posts.Select(p => p.PostId).ToList();
            var postUids = posts.Select(p => p.PostUid).ToList();
            var mediaFileIds = posts
                .Where(p => p.MediaFileId.HasValue)
                .Select(p => p.MediaFileId!.Value)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "[DevTool] Found {PostCount} posts with {MediaCount} media files. UserId={UserId}",
                posts.Count, mediaFileIds.Count, user.Id);

            // ============================================
            // STEP 3: Delete AWS Media Files (before DB records)
            // ============================================
            foreach (var post in posts)
            {
                if (string.IsNullOrEmpty(post.MediaFileUrl))
                    continue;

                try
                {
                    var fileName = post.MediaFileUrl.Substring(
                        post.MediaFileUrl.LastIndexOf('/') + 1);

                    await _fileUploadService.Delete(new FileUploadConfigDto
                    {
                        OldFileName = fileName,
                        BucketName = _configuration[AwsLocationNames.S3UploadBucket],
                        FolderPath = _configuration[AwsLocationNames.PublicUploadFolder],
                    });

                    result.AwsFilesDeleted++;

                    _logger.LogDebug(
                        "[DevTool] Deleted AWS file. PostUid={PostUid}, FileName={FileName}",
                        post.PostUid, fileName);
                }
                catch (Exception ex)
                {
                    result.AwsFilesFailed++;
                    _logger.LogWarning(ex,
                        "[DevTool] Failed to delete AWS file for post {PostUid}, continuing.",
                        post.PostUid);
                }
            }

            // ============================================
            // STEP 4: Delete Related Entities (batch operations)
            // ============================================

            // 4.1 Comment likes — navigate via Comment.PostId (shadow property)
            //     Materialise comment IDs first to avoid shadow-property translation issues
            var commentIds = await _dbContext.Comments
                .Where(c => postIds.Contains(EF.Property<int>(c, "PostId")))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (commentIds.Any())
            {
                result.CommentLikesDeleted = await _dbContext.CommentLikes
                    .Where(cl => commentIds.Contains(cl.CommentId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // 4.2 Comments
            result.CommentsDeleted = await _dbContext.Comments
                .Where(c => postIds.Contains(EF.Property<int>(c, "PostId")))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.3 Post likes
            result.PostLikesDeleted = await _dbContext.PostLikes
                .Where(pl => postIds.Contains(pl.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.4 Profile mentions
            result.RelatedEntitiesDeleted += await _dbContext.PostProfileMentions
                .Where(ppm => postIds.Contains(ppm.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.5 Store mentions
            result.RelatedEntitiesDeleted += await _dbContext.PostStoreMentions
                .Where(psm => postIds.Contains(psm.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.6 Product tags
            result.RelatedEntitiesDeleted += await _dbContext.PostProductTags
                .Where(ppt => postIds.Contains(ppt.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.7 Hashtags
            result.RelatedEntitiesDeleted += await _dbContext.PostHashtags
                .Where(ph => postIds.Contains(ph.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.8 Bookmark collection items
            result.RelatedEntitiesDeleted += await _dbContext.BookmarkCollectionItems
                .Where(bci => postIds.Contains(bci.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.9 Post clicks (uses navigation property)
            result.RelatedEntitiesDeleted += await _dbContext.PostClicks
                .Where(pc => postIds.Contains(pc.Post.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.10 Post my-styles
            result.RelatedEntitiesDeleted += await _dbContext.PostMyStyles
                .Where(pms => postIds.Contains(pms.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.11 Reports (keyed by entity Uid string)
            result.RelatedEntitiesDeleted += await _dbContext.Reports
                .Where(r => postUids.Contains(r.EntityUid))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.12 Stories referencing these posts
            result.RelatedEntitiesDeleted += await _dbContext.Stories
                .Where(s => s.SharedPostId.HasValue && postIds.Contains(s.SharedPostId.Value))
                .ExecuteDeleteAsync(cancellationToken);

            // 4.13 Notification histories
            result.RelatedEntitiesDeleted += await _dbContext.NotificationHistories
                .Where(n => postUids.Contains(n.TargetId) && n.TargetType == EntityTypeEnum.POST)
                .ExecuteDeleteAsync(cancellationToken);

            // ============================================
            // STEP 5: Hard-Delete Posts (before MediaFiles to avoid FK cascade deleting posts)
            // ============================================
            result.PostsDeleted = await _dbContext.Posts
                .Where(p => postIds.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // ============================================
            // STEP 6: Delete MediaFile Records (after posts so cascade does not remove posts first)
            // ============================================
            if (mediaFileIds.Any())
            {
                await _dbContext.MediaFiles
                    .Where(mf => mediaFileIds.Contains(mf.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            stopwatch.Stop();
            result.ElapsedMs = stopwatch.ElapsedMilliseconds;

            _logger.LogWarning(
                "[DevTool] DeleteAllUserPosts completed. " +
                "UserId={UserId}, Posts={PostsDeleted}, AwsDeleted={AwsFilesDeleted}, " +
                "AwsFailed={AwsFilesFailed}, Comments={CommentsDeleted}, " +
                "Related={RelatedEntitiesDeleted}, ElapsedMs={ElapsedMs}",
                user.Id,
                result.PostsDeleted,
                result.AwsFilesDeleted,
                result.AwsFilesFailed,
                result.CommentsDeleted,
                result.RelatedEntitiesDeleted,
                result.ElapsedMs);

            return result;
        }

        private sealed class PostMetadata
        {
            public int PostId { get; set; }
            public string PostUid { get; set; }
            public int? MediaFileId { get; set; }
            public string MediaFileUid { get; set; }
            public string MediaFileUrl { get; set; }
        }
    }
}
