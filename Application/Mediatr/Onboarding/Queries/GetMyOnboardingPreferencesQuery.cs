using AutoMapper;
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
    public class GetMyOnboardingPreferencesQuery : IRequest<OnboardingPreferencesResponse>
    {
    }

    public class GetMyOnboardingPreferencesQueryHandler : IRequestHandler<GetMyOnboardingPreferencesQuery, OnboardingPreferencesResponse>
    {
        private readonly ILogger<GetMyOnboardingPreferencesQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetMyOnboardingPreferencesQueryHandler(ILogger<GetMyOnboardingPreferencesQueryHandler> logger, IMapper mapper, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<OnboardingPreferencesResponse> Handle(GetMyOnboardingPreferencesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var myPreferences = await _dbContext.ProfileOnboardingPreferences.Where(e => e.ProfileId == currentUser.Profile.Id).Select(e => new OnboardingPreferenceResponse()
                {
                    Name = e.OnboardingPreference.Name,
                    Key = e.OnboardingPreference.Key,
                    Description = e.OnboardingPreference.Description,
                    Gender = e.OnboardingPreference.Gender.Key,
                }).ToListAsync(cancellationToken);

                return new OnboardingPreferencesResponse()
                {
                    Preferences = myPreferences
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
