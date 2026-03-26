using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stories.Queries;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shortid;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Stories.Commands.ToggleLike;

public class StoryToggleLikeCommand : IRequest<StoryToggleLikeResponse>
{
    public string StoryUid { get; set; }
}

public class StoryToggleLikeCommandHandler : IRequestHandler<StoryToggleLikeCommand, StoryToggleLikeResponse>
{
    private readonly ILogger<StoryToggleLikeCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public StoryToggleLikeCommandHandler(
        ILogger<StoryToggleLikeCommandHandler> logger,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        INotificationService notificationService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<StoryToggleLikeResponse> Handle(StoryToggleLikeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            if (currentUser.Profile == null)
                throw new BadRequestException($"Profile doesn't exist for user");


            var story = await _dbContext.Stories
                .Include(s => s.SharedProduct)
                .Include(s => s.SharedCollection)
                .Include(s => s.SharedPost)
                .SingleOrDefaultAsync(s => s.IsActive && s.Uid == request.StoryUid, cancellationToken);

            if (story == null)
                throw new NotFoundException("Story not found");

            var existingStoryLike = await _dbContext.StoryLikes.Include(sl => sl.Story)
                .SingleOrDefaultAsync(l => l.Story.Uid == request.StoryUid && l.LikedBy.Uid == currentUser.Profile.Uid, cancellationToken);

            var likedByMe = false;
            if (existingStoryLike == null)
            {
                _dbContext.StoryLikes.Add(new StoryLike
                {
                    Story = story,
                    LikedBy = currentUser.Profile
                });
                likedByMe = true;
                // Choose target type based on story type
                var targetType = story.StoryType == StoryTypeEnum.Product ? EntityTypeEnum.PRODUCT :
                                 story.StoryType == StoryTypeEnum.Collection ? EntityTypeEnum.COLLECTION :
                                 story.StoryType == StoryTypeEnum.Post ? EntityTypeEnum.POST :
                                 EntityTypeEnum.STORY;
                var targetId = story.StoryType == StoryTypeEnum.Product ? story.SharedProduct?.Uid :
                               story.StoryType == StoryTypeEnum.Collection ? story.SharedCollection?.Uid :
                               story.StoryType == StoryTypeEnum.Post ? story.SharedPost?.Uid :
                               story.Uid;
                await _notificationService.SaveLikeNotificationAsync(currentUser.Id, targetId, targetType, ActivityActionTypeEnum.LikeStory);
            }
            else
            {
                _dbContext.StoryLikes.Remove(existingStoryLike);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return new StoryToggleLikeResponse
            {
                LikedByMe = likedByMe,
                LikesCount = await _dbContext.StoryLikes
                    .Where(pl => pl.StoryId == story.Id).CountAsync(cancellationToken)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggling like for story");
            throw;
        }
    }
}