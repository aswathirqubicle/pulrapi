using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Search.Queries;

public class GetSearchHistorySearchQuery : IRequest<List<string>>
{
}

public class GetSearchHistorySearchQueryHandler : IRequestHandler<GetSearchHistorySearchQuery, List<string>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetSearchHistorySearchQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetSearchHistorySearchQueryHandler(
        IApplicationDbContext dbContext,
        ILogger<GetSearchHistorySearchQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<List<string>> Handle(GetSearchHistorySearchQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.SearchHistories
                .OrderByDescending(sh => sh.CreatedAt).ThenBy(sh => sh.SearchCount)
                .Where(s => s.IsActive
                            && s.UserId == _currentUserService.GetUserId())
                .Select(s => s.Term)
                .Take(5)
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting users search history");
            throw;
        }
    }
}