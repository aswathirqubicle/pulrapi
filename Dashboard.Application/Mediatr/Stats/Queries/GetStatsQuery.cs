using Dashboard.Application.Models.Stats;
using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Mediatr.Stats.Queries;

public class GetStatsQuery : IRequest<StatsResponse>
{
}

public class GetStatsQueryHandler : IRequestHandler<GetStatsQuery, StatsResponse>
{
    private readonly ILogger<GetStatsQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;

    public GetStatsQueryHandler(ILogger<GetStatsQueryHandler> logger, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<StatsResponse> Handle(GetStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return new StatsResponse()
            {
                UsersTotalCount = await _dbContext.Users.CountAsync(),
                ProfilesCount = await _dbContext.Profiles.Where(p => p.IsActive).CountAsync(),
                StoresCount = await _dbContext.Stores.Where(s => s.IsActive).CountAsync(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
