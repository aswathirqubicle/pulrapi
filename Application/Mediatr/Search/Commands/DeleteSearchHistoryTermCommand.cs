using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Search.Commands;

public class DeleteSearchHistoryTermCommand : IRequest <Unit>
{
    public string Term { get; set; }
}

public class DeleteSearchHistoryTermCommandHandler : IRequestHandler<DeleteSearchHistoryTermCommand,Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeleteSearchHistoryTermCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeleteSearchHistoryTermCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<DeleteSearchHistoryTermCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }


    public async Task<Unit> Handle(DeleteSearchHistoryTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var searchHistoryTerm = await _dbContext.SearchHistories
                .OrderByDescending(sh => sh.CreatedAt)
                .FirstOrDefaultAsync(sh => sh.IsActive && sh.UserId == _currentUserService.GetUserId(),
                    cancellationToken);

            searchHistoryTerm.IsActive = false;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting search term");
            throw;
        }
    }
}