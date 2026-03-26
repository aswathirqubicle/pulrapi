using System.Linq;
using System.Threading.Tasks;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Core.Application.Models.External.Apple;
using System.Collections.Generic;

namespace Core.Application.Interfaces
{
    public interface IUserService
    {
        Task<string> GetRoleIdAsync(string roleName);
        IQueryable<IdentityUserRole<string>> IsUserInRoleQuery(string userId, string roleId);
        Task<LoginResponse> LoginAsync(LoginDto loginDto);
        Task<UserRegisterResponseDto> RegisterAsync(UserRegisterDto model);
        Task<AuthModel> GetTokenAsync(TokenRequest request);
        Task DeleteAccountAsync(User currentUser);
        Task ReactivateUserAsync(User user);
        Task DeactivateAccountAsync(User user);
        Task ManagePasswordResetRequest(string email);
        Task AssignRole(string storeOwner);
        Task<LoginResponse> LoginWithFacebookAsync(string accessToken);
        Task<LoginResponse> LoginWithGoogleAsync(string accessToken, string firstName = null, string lastName = null, string pictureUrl = null, bool isEmailVerified = false, DeviceDto device = null);
        Task<LoginResponse> LoginWithAppleAsync(string identityToken, AppleNameInfo fullName = null, DeviceDto device = null);
        Task SendEmailConfirmationToken(User user);
        Task<List<LoginActivityDto>> GetLoginActivityAsync();
        Task<List<RecognisedDeviceDto>> GetRecognisedDevicesAsync();
        Task SignOutDeviceAsync(string deviceIdentifier);
        Task SignOutAllDevicesAsync(string currentDeviceIdentifier);
        Task<UserNotificationSettingDto> GetNotificationSettingsAsync(string deviceId, string pushToken);
        Task<UserNotificationSettingDto> UpdateNotificationSettingsAsync(UserNotificationSettingDto dto);
        Task CreateNotificationSettingsForDeviceAsync(string deviceId, string pushToken);
        Task SaveLoginActivityAsync(string userId, string brand, string modelName, string osVersion, string deviceIdentifier, string appVersion, string action);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken, string deviceIdentifier);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task<UserLoginActivity> GetLatestLoginActivityAsync(string userId, string deviceIdentifier);
        Task CleanupAllPushTokensForUserAsync(string userId);
    }
}
