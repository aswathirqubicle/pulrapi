using Core.Application.Hubs;
using Dashboard.Application.Models.AppLogs;

namespace Dashboard.Application.Hubs
{
    public class AppLogsNotification : SignalrNotificationBase
    {
        public List<AppLogResponse>? AppLogs { get; set; }
    }
}
