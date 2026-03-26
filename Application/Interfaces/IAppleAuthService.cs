using System.Threading.Tasks;
using Core.Application.Models.External.Apple;

namespace Core.Application.Interfaces
{
    public interface IAppleAuthService
    {
        Task<AppleUserInfo> GetUserInfoAsync(string identityToken, string fullResponse = null);
        Task<bool> ValidateIdentityTokenAsync(string identityToken);
    }
}