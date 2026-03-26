using System;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApi.Middleware
{
    public class UserEngagementMiddleware(
        ILogger<UserEngagementMiddleware> logger,
        ICurrentUserService currentUserService
        ) : IMiddleware
    {
        private readonly ILogger<UserEngagementMiddleware> _logger = logger;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _logger.LogInformation("UserEngagementMiddleware processing {Path} at {Time}", context.Request.Path, DateTime.UtcNow);
            
            // Skip for anonymous endpoints
            if (context.Request.Path.StartsWithSegments("/api/test") || context.Request.Path.StartsWithSegments("/swagger"))
            {
                _logger.LogInformation("Bypassing UserEngagementMiddleware for {Path}", context.Request.Path);
                await next(context);
                return;
            }

            try
            {
                // Only process if user is authenticated
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("Processing engagement for authenticated user");
                    // TODO: Add engagement logic using _currentUserService
                }
                else
                {
                    _logger.LogDebug("No authenticated user for {Path}", context.Request.Path);
                }
                await next(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in UserEngagementMiddleware for {Path}: {Message}", context.Request.Path, e.Message);
                throw;
            }
        }
    }
}
