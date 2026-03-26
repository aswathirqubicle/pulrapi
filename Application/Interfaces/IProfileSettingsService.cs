using System.Threading.Tasks;
using Core.Application.DTOs;

namespace Core.Application.Interfaces
{
    public interface IProfileSettingsService
    {
        Task<ProfileSettingsDto> GetProfileSettingsAsync();
        Task<ProfileSettingsDto> UpdateProfileSettingsAsync(UpdateProfileSettingsDto settingsDto);
    }
} 