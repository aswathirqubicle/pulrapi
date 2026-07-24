using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;
using Core.Domain.Enums;

namespace Core.WebApi.BackgroundServices
{
    public class RefundEscalationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefundEscalationService> _logger;
        private Timer _timer;

        public RefundEscalationService(
            IServiceProvider serviceProvider,
            ILogger<RefundEscalationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Schedule to run once per day at 2:00 AM UTC
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(2); // Tomorrow at 2:00 AM
            var delay = nextRun - now;

            _timer = new Timer(DoWork, null, delay, TimeSpan.FromDays(1));
            _logger.LogInformation("RefundEscalationService scheduled to run daily at 2:00 AM UTC. First run in {Delay}.", delay);

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                _logger.LogInformation("RefundEscalationService is running...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var settingsCacheService = scope.ServiceProvider.GetRequiredService<ISettingsCacheService>();

                    var platformSettings = await settingsCacheService.GetPlatformSettingsAsync();
                    var responseDays = platformSettings?.RefundResponseDays ?? 7;

                    var cutoffDate = DateTime.UtcNow.AddDays(-responseDays);

                    var overdueDisputes = await dbContext.RefundDisputes
                        .Include(rd => rd.OrderProductAffiliate)
                            .ThenInclude(opa => opa.Order)
                                .ThenInclude(o => o.Profile)
                                    .ThenInclude(p => p.User)
                        .Include(rd => rd.BuyerProfile)
                            .ThenInclude(bp => bp.User)
                        .Include(rd => rd.SellerProfile)
                            .ThenInclude(sp => sp.User)
                        .Where(rd => rd.Status == DisputeStatusEnum.Pending && rd.CreatedAt < cutoffDate)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} overdue refund disputes to escalate.", overdueDisputes.Count);

                    foreach (var dispute in overdueDisputes)
                    {
                        try
                        {
                            // Update dispute status
                            dispute.Status = DisputeStatusEnum.UnderReview;
                            dispute.UpdatedAt = DateTime.UtcNow;

                            // Update order item
                            var orderItem = dispute.OrderProductAffiliate;
                            orderItem.EscrowStatus = EscrowStatusEnum.RefundRejected;
                            orderItem.UpdatedAt = DateTime.UtcNow;

                            // Save
                            await dbContext.SaveChangesAsync(CancellationToken.None);

                            // Notify buyer
                            if (dispute.BuyerProfile != null)
                            {
                                await notificationService.SaveRefundDisputedNotificationAsync(
                                    dispute.BuyerProfile.Id,
                                    dispute.SellerProfile?.Id,
                                    orderItem.Uid);
                            }

                            _logger.LogInformation(
                                "Auto-escalated refund dispute {DisputeUid} for order item {ItemUid}. Seller did not respond within {Days} days.",
                                dispute.Uid, orderItem.Uid, responseDays);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error escalating refund dispute {DisputeUid}", dispute.Uid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefundEscalationService");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }
    }
}
