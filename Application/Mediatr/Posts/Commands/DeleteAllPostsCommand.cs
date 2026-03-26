using MediatR;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class DeleteAllPostsCommand : IRequest<DeleteAllPostsResult> { }

    public class DeleteAllPostsResult
    {
        public int PostsDeleted { get; set; }
        public int AwsFilesDeleted { get; set; }
        public int AwsFilesFailed { get; set; }
        public int CommentLikesDeleted { get; set; }
        public int CommentsDeleted { get; set; }
        public int PostLikesDeleted { get; set; }
        public int RelatedEntitiesDeleted { get; set; }
        public long ElapsedMs { get; set; }
    }

    public class DeleteAllPostsCommandHandler : IRequestHandler<DeleteAllPostsCommand, DeleteAllPostsResult>
    {
        private readonly ILogger<DeleteAllPostsCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IFileUploadService _fileUploadService;

        public DeleteAllPostsCommandHandler(
            ILogger<DeleteAllPostsCommandHandler> logger,
            IApplicationDbContext dbContext,
            IConfiguration configuration,
            IFileUploadService fileUploadService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _fileUploadService = fileUploadService;
        }

        public async Task<DeleteAllPostsResult> Handle(
            DeleteAllPostsCommand request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new DeleteAllPostsResult();

            _logger.LogWarning("[DevTool] DeleteAllPosts started — this will delete EVERY post in the database.");

            // ============================================
            // STEP 1: Load All Post Metadata (IDs + media)
            // ============================================
            var posts = await _dbContext.Posts
                .Select(p => new PostSnapshot
                {
                    PostId = p.Id,
                    PostUid = p.Uid,
                    MediaFileId = (int?)p.MediaFile.Id,
                    MediaFileUrl = p.MediaFile != null ? p.MediaFile.Url : null,
                })
                .ToListAsync(cancellationToken);

            if (!posts.Any())
            {
                _logger.LogInformation("[DevTool] No posts found in the database.");
                return result;
            }

            var postIds = posts.Select(p => p.PostId).ToList();
            var postUids = posts.Select(p => p.PostUid).ToList();
            var mediaFileIds = posts
                .Where(p => p.MediaFileId.HasValue)
                .Select(p => p.MediaFileId!.Value)
                .Distinct()
                .ToList();

            _logger.LogWarning(
                "[DevTool] Found {PostCount} posts with {MediaCount} media files to purge.",
                posts.Count, mediaFileIds.Count);

            // ============================================
            // STEP 2: Delete AWS S3 Files (before DB records)
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
            // STEP 3: Delete Related Entities (batch, correct FK order)
            // ============================================

            // 3.1 Comment likes — materialise comment IDs first (shadow PostId property)
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

            // 3.2 Comments
            result.CommentsDeleted = await _dbContext.Comments
                .Where(c => postIds.Contains(EF.Property<int>(c, "PostId")))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.3 Post likes
            result.PostLikesDeleted = await _dbContext.PostLikes
                .Where(pl => postIds.Contains(pl.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.4 Profile mentions
            result.RelatedEntitiesDeleted += await _dbContext.PostProfileMentions
                .Where(ppm => postIds.Contains(ppm.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.5 Store mentions
            result.RelatedEntitiesDeleted += await _dbContext.PostStoreMentions
                .Where(psm => postIds.Contains(psm.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.6 Product tags
            result.RelatedEntitiesDeleted += await _dbContext.PostProductTags
                .Where(ppt => postIds.Contains(ppt.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.7 Hashtags
            result.RelatedEntitiesDeleted += await _dbContext.PostHashtags
                .Where(ph => postIds.Contains(ph.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.8 Bookmark collection items
            result.RelatedEntitiesDeleted += await _dbContext.BookmarkCollectionItems
                .Where(bci => postIds.Contains(bci.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.9 Post clicks (navigation property)
            result.RelatedEntitiesDeleted += await _dbContext.PostClicks
                .Where(pc => postIds.Contains(pc.Post.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.10 Post my-styles
            result.RelatedEntitiesDeleted += await _dbContext.PostMyStyles
                .Where(pms => postIds.Contains(pms.PostId))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.11 Reports scoped to posts (by entity Uid)
            result.RelatedEntitiesDeleted += await _dbContext.Reports
                .Where(r => postUids.Contains(r.EntityUid))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.12 Stories that reference posts as shared content
            result.RelatedEntitiesDeleted += await _dbContext.Stories
                .Where(s => s.SharedPostId.HasValue && postIds.Contains(s.SharedPostId.Value))
                .ExecuteDeleteAsync(cancellationToken);

            // 3.13 Notification histories for posts
            result.RelatedEntitiesDeleted += await _dbContext.NotificationHistories
                .Where(n => postUids.Contains(n.TargetId) && n.TargetType == EntityTypeEnum.POST)
                .ExecuteDeleteAsync(cancellationToken);

            // ============================================
            // STEP 4: Hard-Delete Posts (before MediaFiles to avoid FK cascade deleting posts)
            // ============================================
            result.PostsDeleted = await _dbContext.Posts
                .Where(p => postIds.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken);

            // ============================================
            // STEP 5: Delete MediaFile Records (after posts so cascade does not remove posts first)
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
                "[DevTool] DeleteAllPosts completed. " +
                "Posts={PostsDeleted}, AwsDeleted={AwsFilesDeleted}, AwsFailed={AwsFilesFailed}, " +
                "Comments={CommentsDeleted}, Related={RelatedEntitiesDeleted}, ElapsedMs={ElapsedMs}",
                result.PostsDeleted,
                result.AwsFilesDeleted,
                result.AwsFilesFailed,
                result.CommentsDeleted,
                result.RelatedEntitiesDeleted,
                result.ElapsedMs);

            return result;
        }

        private sealed class PostSnapshot
        {
            public int PostId { get; set; }
            public string PostUid { get; set; }
            public int? MediaFileId { get; set; }
            public string MediaFileUrl { get; set; }
        }
    }
}
