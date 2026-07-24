using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;

namespace Core.Application.Mediatr.PlatformSettings.Queries
{
    public class GetPlatformSettingsQuery : IRequest<PlatformSetting>
    {
    }

    public class GetPlatformSettingsQueryHandler : IRequestHandler<GetPlatformSettingsQuery, PlatformSetting>
    {
        private readonly ISettingsCacheService _settingsCacheService;

        public GetPlatformSettingsQueryHandler(ISettingsCacheService settingsCacheService)
        {
            _settingsCacheService = settingsCacheService;
        }

        public async Task<PlatformSetting> Handle(GetPlatformSettingsQuery request, CancellationToken cancellationToken)
        {
            return await _settingsCacheService.GetPlatformSettingsAsync();
        }
    }
}