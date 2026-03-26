using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Core.Application.Mediatr.Stories.Commands.Delete;

public class DeleteStoryCommand : IRequest <Unit>
{
    public string Uid { get; set; }
}

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand,Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeleteStoryCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeleteStoryCommandHandler(IApplicationDbContext dbContext, ILogger<DeleteStoryCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();

            var story = await _dbContext.Stories.SingleOrDefaultAsync(s => s.IsActive && s.Uid == request.Uid, cancellationToken);

            if (story == null)
                throw new NotFoundException("Story not found");

            if (story.UserId != currentUser.Id)
                throw new ForbiddenException("You do not have permission to delete this story.");

            // Remove related notifications
            var notifications = await _dbContext.NotificationHistories
                .Where(n => n.TargetId == request.Uid && n.TargetType == Core.Domain.Enums.EntityTypeEnum.STORY)
                .ToListAsync(cancellationToken);
            if (notifications.Any())
            {
                _dbContext.NotificationHistories.RemoveRange(notifications);
            }

            _dbContext.Stories.Remove(story);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting the story");
            throw;
        }
    }
}