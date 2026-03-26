using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IExpoNotificationService
    {
        Task SendNotificationAsync(string expoToken, string title, string body, object data = null);
        Task SendNotificationsAsync(List<string> expoTokens, string title, string body, object data = null);
        bool ValidateToken(string expoToken);
    }
} 