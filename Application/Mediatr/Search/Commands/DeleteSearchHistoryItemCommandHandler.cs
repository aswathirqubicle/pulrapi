using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Search.Commands
{
    public class DeleteSearchHistoryItemCommandHandler : IRequestHandler<DeleteSearchHistoryItemCommand, Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public DeleteSearchHistoryItemCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DeleteSearchHistoryItemCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserId();

            // Find the history entry by Id and UserId
            var historyEntry = await _dbContext.SearchHistories
                .FirstOrDefaultAsync(h => h.Uid == request.Id && h.User.Id == userId, cancellationToken);

            if (historyEntry == null)
            {
                // Handle case where history entry is not found or doesn't belong to the user
                // You might throw a specific exception or return a different result
                return Unit.Value; // Or throw an exception like NotFoundException
            }

            historyEntry.IsActive = false;
            historyEntry.SearchCount = 0;
            historyEntry.UpdatedAt = DateTime.UtcNow; // Update the timestamp
            _dbContext.SearchHistories.Update(historyEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
} 