using Core.Application.Interfaces;
using Core.Application.Models.Onboarding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Queries
{
    public class GetAllOnboardingPreferencesQuery : IRequest<VibesResponse>
    {
    }

    public class GetAllOnboardingPreferencesQueryHandler : IRequestHandler<GetAllOnboardingPreferencesQuery, VibesResponse>
    {
        private readonly ILogger<GetAllOnboardingPreferencesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetAllOnboardingPreferencesQueryHandler(ILogger<GetAllOnboardingPreferencesQueryHandler> logger, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<VibesResponse> Handle(GetAllOnboardingPreferencesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var vibes = await _dbContext.Vibes
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Category)
                    .ThenBy(v => v.DisplayOrder)
                    .Select(v => new VibeResponse
                    {
                        Uid = v.Uid,
                        Name = v.Name,
                        Key = v.Key,
                        Category = v.Category,
                        DisplayOrder = v.DisplayOrder,
                        IsActive = v.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var response = new VibesResponse();
                
                var categories = vibes.GroupBy(v => v.Category).ToList();
                
                foreach (var category in categories)
                {
                    var categoryResponse = new VibeCategoryResponse
                    {
                        CategoryName = category.Key,
                        Vibes = category.OrderBy(v => v.DisplayOrder).ToList()
                    };
                    
                    response.Categories.Add(categoryResponse);
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
