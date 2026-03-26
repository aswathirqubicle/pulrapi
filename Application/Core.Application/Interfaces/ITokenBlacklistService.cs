using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string token);
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task RemoveTokenAsync(string token);
    }
} 