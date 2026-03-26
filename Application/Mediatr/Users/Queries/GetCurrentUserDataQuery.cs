using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Queries;
using Core.Application.Models.Currencies;
using Core.Application.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Users.Queries
{
    public class GetCurrentUserDataQuery : IRequest<LoginResponse> { }

    public class GetCurrentUserDataQueryHandler : IRequestHandler<GetCurrentUserDataQuery, LoginResponse>
    {
        private readonly ILogger<GetCurrentUserDataQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetCurrentUserDataQueryHandler(ILogger<GetCurrentUserDataQueryHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IMapper mapper)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<LoginResponse> Handle(GetCurrentUserDataQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                // Check onboarding completion status
                var onboardingPreferencesCount = await _dbContext.ProfileOnboardingPreferences
                    .Where(p => p.ProfileId == cUser.Profile.Id)
                    .CountAsync(cancellationToken);

                var loginResponse = new LoginResponse()
                {
                    Id = cUser.Id,
                    ProfileUid = cUser.Profile.Uid,
                    Roles = cUser.Roles.ToList(),
                    Token = _currentUserService.GetToken(),
                    Username = cUser.UserName,
                    Email = cUser.Email,
                    ImageUrl = cUser.Profile.ImageUrl,
                    FullName = cUser.FirstName,
                    FirstName = cUser.FirstName,
                    LastName = cUser.LastName,
                    PhoneNumber = cUser.PhoneNumber,
                    StoreUids = _dbContext.Stores.Where(s => s.UserId == cUser.Id).Select(s => s.Uid).ToList(),
                    Currency = _mapper.Map<CurrencyDetailsResponse>(cUser.Profile.Currency),
                    HasCompletedOnboarding = onboardingPreferencesCount > 0
                };

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
