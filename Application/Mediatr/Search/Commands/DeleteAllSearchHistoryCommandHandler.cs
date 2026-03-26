using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Search.Commands
{
    public class DeleteAllSearchHistoryCommandHandler : IRequestHandler<DeleteAllSearchHistoryCommand, Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public DeleteAllSearchHistoryCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DeleteAllSearchHistoryCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserId();

            var historyEntries = await _dbContext.SearchHistories
                .Where(h => h.User.Id == userId)
                .ToListAsync(cancellationToken);

            foreach(var entry in historyEntries)
            {
                entry.IsActive = false; 
            }

            _dbContext.SearchHistories.UpdateRange(historyEntries);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
} 