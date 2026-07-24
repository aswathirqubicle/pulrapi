using Hangfire;
using Core.Application.Interfaces;
using Core.Application.Services;

namespace Core.Infrastructure.Services.Cron
{
    public class HangfireJobScheduler
    {
        public static void ScheduleRecurringJobs(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.AddOrUpdate<IExchangeRateService>(nameof(IExchangeRateService), job => job.GetExchangeRates(), HourInterval(12));

            // Clean up stale push tokens every 6 hours to prevent notifications to logged out devices
            recurringJobManager.AddOrUpdate<INotificationService>("CleanupStalePushTokens",
                job => job.CleanupAllStalePushTokensAsync(),
                "0 */6 * * *"); // Every 6 hours

            // Purge expired deleted posts daily at 2 AM
            recurringJobManager.AddOrUpdate<IPostPurgeService>("PurgeExpiredDeletedPosts",
                job => job.PurgeExpiredDeletedPostsAsync(),
                "0 2 * * *"); // Daily at 2 AM

            // Delete expired stories every hour
            recurringJobManager.AddOrUpdate<IStoryCleanupService>("DeleteExpiredStories",
                job => job.DeleteExpiredStoriesAsync(),
                "0 * * * *"); // Every hour

            // Purge expired revoked-token (logout blacklist) rows daily at 3 AM
            recurringJobManager.AddOrUpdate<ITokenBlacklistService>("PurgeExpiredRevokedTokens",
                job => job.PurgeExpiredAsync(),
                "0 3 * * *"); // Daily at 3 AM
        }

        public static string HourInterval(int interval)
        {
            return string.Format("0 */{0} * * *", (object)interval);
        }
    }
}
