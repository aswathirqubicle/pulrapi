using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Core.Application.Security.Validation.Services;
using Core.Application.Behaviors;
using MediatR;
using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering optimized validation services
    /// </summary>
    public static class OptimizedValidationExtensions
    {
        /// <summary>
        /// Registers optimized validation services with dependency injection
        /// </summary>
        public static IServiceCollection AddOptimizedValidation(this IServiceCollection services)
        {
            // Register the optimized validation service as singleton for better performance
            services.AddSingleton<OptimizedValidationService>();

            // Register the optimized validation behavior
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(OptimizedValidationBehavior<,>));

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(typeof(OptimizedValidationExtensions).Assembly);

            return services;
        }

        /// <summary>
        /// Registers validation services with performance monitoring
        /// </summary>
        public static IServiceCollection AddOptimizedValidationWithMonitoring(this IServiceCollection services)
        {
            services.AddOptimizedValidation();

            // Add performance monitoring for validation cache
            services.AddHostedService<ValidationCacheMonitoringService>();

            return services;
        }
    }

    /// <summary>
    /// Background service for monitoring validation cache performance
    /// </summary>
    public class ValidationCacheMonitoringService : BackgroundService
    {
        private readonly ILogger<ValidationCacheMonitoringService> _logger;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(5);

        public ValidationCacheMonitoringService(ILogger<ValidationCacheMonitoringService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var stats = OptimizedValidationService.GetCacheStats();
                    _logger.LogInformation("Validation Cache Stats - Count: {Count}, Memory Estimate: {MemoryEstimate} bytes", 
                        stats.Count, stats.MemoryEstimate);

                    // Clear cache if it gets too large
                    if (stats.Count > 10000)
                    {
                        OptimizedValidationService.ClearCache();
                        _logger.LogWarning("Validation cache cleared due to size limit");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring validation cache");
                }

                await Task.Delay(_monitoringInterval, stoppingToken);
            }
        }
    }
}
