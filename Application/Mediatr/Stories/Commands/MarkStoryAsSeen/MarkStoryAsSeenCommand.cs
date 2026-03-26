using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Stories.Commands.MarkStoryAsSeen
{
    public class MarkStoryAsSeenCommand : IRequest <Unit>
    {
        public string StoryUid { get; set; }
    }

    public class MarkStoryAsSeenCommandHandler : IRequestHandler<MarkStoryAsSeenCommand,Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public MarkStoryAsSeenCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(MarkStoryAsSeenCommand request, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var story = await _dbContext.Stories
                .Include(s => s.StorySeens)
                .Include(s => s.User)
                .Include(s => s.Store)
                .SingleOrDefaultAsync(s => s.Uid == request.StoryUid, cancellationToken);

            if (story == null)
                throw new NotFoundException("Story not found");

            // Check if user is trying to mark their own story as seen
            // Note: Currently allowing users to mark their own stories as seen
            // bool isOwnStory = false;
            // if (story.User != null && story.User.Id == currentUser.Id)
            // {
            //     isOwnStory = true;
            // }
            // else if (story.Store != null && story.Store.UserId == currentUser.Id)
            // {
            //     isOwnStory = true;
            // }

            var alreadySeen = await _dbContext.StorySeens.AnyAsync(s => s.StoryId == story.Id && s.SeenById == currentUser.Profile.Id, cancellationToken);

            if (!alreadySeen)
            {
                _dbContext.StorySeens.Add(new StorySeen
                {
                    StoryId = story.Id,
                    SeenById = currentUser.Profile.Id,
                    SeenAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}