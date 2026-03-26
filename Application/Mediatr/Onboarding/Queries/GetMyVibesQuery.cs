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
    public class GetMyVibesQuery : IRequest<VibesResponse>
    {
    }

    public class GetMyVibesQueryHandler : IRequestHandler<GetMyVibesQuery, VibesResponse>
    {
        private readonly ILogger<GetMyVibesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetMyVibesQueryHandler(ILogger<GetMyVibesQueryHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<VibesResponse> Handle(GetMyVibesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var myVibes = await _dbContext.ProfileVibes
                    .Where(pv => pv.ProfileId == currentUser.Profile.Id)
                    .Select(pv => new VibeResponse
                    {
                        Uid = pv.Vibe.Uid,
                        Name = pv.Vibe.Name,
                        Key = pv.Vibe.Key,
                        Category = pv.Vibe.Category,
                        DisplayOrder = pv.Vibe.DisplayOrder,
                        IsActive = pv.Vibe.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var response = new VibesResponse();
                
                var categories = myVibes.GroupBy(v => v.Category).ToList();
                
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
