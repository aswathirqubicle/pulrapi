using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Tag.Queries;

namespace Core.Application.Mediatr.Tag.Queries
{
    public class GetPopularTagsQuery : IRequest<List<string>>
    {
    }

    public class GetPopularTagsQueryHandler : IRequestHandler<GetPopularTagsQuery, List<string>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetPopularTagsQueryHandler> _logger;

        public GetPopularTagsQueryHandler(IApplicationDbContext dbContext, ILogger<GetPopularTagsQueryHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<string>> Handle(GetPopularTagsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.Hashtags
                    .OrderByDescending(e => e.PostHashtags.Count())
                    .Take(16)
                    .Select(e => e.Value).Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
