using Hangfire.Dashboard;
using System.Diagnostics.CodeAnalysis;

namespace Core.Application.Security
{
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}
