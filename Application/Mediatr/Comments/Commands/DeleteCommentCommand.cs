using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Comments.Commands;
using System.Linq;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Mediatr.Comments.Commands
{
    public class DeleteCommentCommand : IRequest<DeleteCommentResponse>
    {
        [SafeUid(allowNullValue:false,maxLength:50,minLength:5,ErrorMessage = "CommentUid contains invalid characters or format.")]
        public string CommentUid { get; set; }
    }

    public class DeleteCommentResponse
    {
        public int TotalCommentsCount { get; set; }
        public string PostOwnerProfileId { get; set; }
        public string Message { get; set; } = "Comment deleted successfully";
    }

    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, DeleteCommentResponse>
    {
        private readonly ILogger<DeleteCommentCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public DeleteCommentCommandHandler(ILogger<DeleteCommentCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<DeleteCommentResponse> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                // Get comment with all related data in one query
                var comment = await _dbContext.Comments
                    .Include(c => c.Post)
                    .Include(c => c.Product)
                    .Include(c => c.CommentLikes)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.CommentLikes)
                    .SingleOrDefaultAsync(c => c.Uid == request.CommentUid, cancellationToken);

                if (comment == null)
                {
                    throw new BadRequestException($"Comment with uid '{request.CommentUid}' doesnt exist");
                }

                // Check if current user is the comment author or the post owner
                bool isCommentAuthor = comment.CommentedBy != null && comment.CommentedBy.UserId == cUser.Id;
                bool isPostOwner = comment.Post != null && comment.Post.User != null && comment.Post.User.Id == cUser.Id;

                if (!isCommentAuthor && !isPostOwner)
                {
                    throw new ForbiddenException("You do not have permission to delete this comment.");
                }

                // Store info for total count
                var postUid = comment.Post?.Uid;
                var productUid = comment.Product?.Uid;

                try
                {
                    // Remove related notifications for this comment
                    var notifications = await _dbContext.NotificationHistories
                        .Where(n => n.TargetId == postUid && n.TargetType == Core.Domain.Enums.EntityTypeEnum.COMMENT)
                        .ToListAsync(cancellationToken);
                    if (notifications.Any())
                    {
                        _dbContext.NotificationHistories.RemoveRange(notifications);
                    }

                    // 1. Delete all reply likes first
                    if (comment.Replies != null)
                    {
                        foreach (var reply in comment.Replies)
                        {
                            if (reply.CommentLikes != null)
                            {
                                _dbContext.CommentLikes.RemoveRange(reply.CommentLikes);
                            }
                        }
                    }

                    // 2. Delete all replies
                    if (comment.Replies != null)
                    {
                        _dbContext.Comments.RemoveRange(comment.Replies);
                    }

                    // 3. Delete main comment's likes
                    if (comment.CommentLikes != null)
                    {
                        _dbContext.CommentLikes.RemoveRange(comment.CommentLikes);
                    }

                    // 4. Delete the main comment
                    _dbContext.Comments.Remove(comment);

                    // Save all changes at once
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting comment and its relations");
                    throw;
                }

                // Get updated total count
                var totalCommentsCount = await _dbContext.Comments
                    .Where(c => (postUid != null && c.Post.Uid == postUid) ||
                               (productUid != null && c.Product.Uid == productUid))
                    .CountAsync(cancellationToken);

                // Get the post owner's profile ID
                var postOwnerProfileId = comment.Post?.User?.Profile?.Uid ?? "";

                return new DeleteCommentResponse { TotalCommentsCount = totalCommentsCount, PostOwnerProfileId = postOwnerProfileId, Message = "Comment deleted successfully" };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
