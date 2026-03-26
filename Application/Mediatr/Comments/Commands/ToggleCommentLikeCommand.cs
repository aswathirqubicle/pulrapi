using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Comments.Queries;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Comments.Commands;

public class ToggleCommentLikeCommand : IRequest<CommentToggleLikeResponse>
{
    public String Uid { get; set; }
}

public class ToggleCommentLikeCommandHandler(
    ILogger<ToggleCommentLikeCommandHandler> logger,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext,
    INotificationService notificationService
        ) : IRequestHandler<ToggleCommentLikeCommand, CommentToggleLikeResponse>
{
    private readonly ILogger<ToggleCommentLikeCommandHandler> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly INotificationService _notificationService = notificationService;

    public async Task<CommentToggleLikeResponse> Handle(ToggleCommentLikeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();


            if (cUser.Profile == null)
                throw new BadRequestException($"Comment doesnt exist for user '{cUser.Id}' .");

            var comment = await _dbContext.Comments.SingleOrDefaultAsync(c => c.Uid == request.Uid, cancellationToken);

            if (comment == null)
                throw new BadRequestException($"Comment doesn't exist.");


            var existingCommentLike = await _dbContext.CommentLikes
                .Include(pl => pl.Comment)
                .SingleOrDefaultAsync(l => l.Comment.Uid == request.Uid && l.LikedBy.Uid == cUser.Profile.Uid, cancellationToken);

            var likedByMe = false;
            if (existingCommentLike == null)
            {
                _dbContext.CommentLikes.Add(new CommentLike { Comment = comment, LikedBy = cUser.Profile });
                await _notificationService.SaveLikeNotificationAsync(cUser.Id, request.Uid, EntityTypeEnum.COMMENT, ActivityActionTypeEnum.LikeComment);
                likedByMe = true;
            }
            else
            {
                _dbContext.CommentLikes.Remove(existingCommentLike);
            }

            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return new CommentToggleLikeResponse
            {
                LikesCount = await _dbContext.CommentLikes.Where(c => c.CommentId == comment.Id).CountAsync(cancellationToken),
                LikedByMe = likedByMe
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling like for a comment");
            throw;
        }
    }
}