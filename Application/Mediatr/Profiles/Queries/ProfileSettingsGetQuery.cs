using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class ProfileSettingsGetQuery : IRequest<MyProfileDetailsResponse>
    {
    }

    public class ProfileSettingsGetQueryHandler : IRequestHandler<ProfileSettingsGetQuery, MyProfileDetailsResponse>
    {
        private readonly ILogger<ProfileSettingsGetQueryHandler> _logger;
        private readonly IProfileService _profileService;

        public ProfileSettingsGetQueryHandler(ILogger<ProfileSettingsGetQueryHandler> logger,
            IProfileService profileService)
        {
            _logger = logger;
            _profileService = profileService;
        }

        public async Task<MyProfileDetailsResponse> Handle(ProfileSettingsGetQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _profileService.GetMy();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
