using Core.Application;
using Core.Application.Hubs;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Security;
using Core.Domain.Entities;
using Core.Infrastructure;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Services.Cron;
using Core.Infrastructure.Swagger;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using WebApi.Configurations.AutoMapper;
using WebApi.Configurations.NLog;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// NLog config
NLogSetup.Configure(builder.Configuration);

// Service registration (from Startup.ConfigureServices)
builder.Services.AddApplication(builder.Configuration);
// builder.Services.AddDashboardApplication(builder.Configuration); // Disabled - Dashboard not currently used
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment, "_myAllowSpecificOrigins");
builder.Services.AddAutoMapper(MappingRegistrationFromMultipleAssembiles.GetAssemblies());
builder.Services.AddTransient<UserEngagementMiddleware>();
builder.Services.AddControllers();

// Rate limiting for auth/credential endpoints (brute-force throttling).
// NOTE: in-memory and therefore per-instance; the cross-instance guarantee
// against account brute-force is the DB-backed Identity lockout.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string ClientKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    static FixedWindowRateLimiterOptions Window(int permitLimit, TimeSpan window) =>
        new() { PermitLimit = permitLimit, Window = window, QueueLimit = 0 };

    options.AddPolicy("auth-login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => Window(10, TimeSpan.FromMinutes(1))));
    options.AddPolicy("auth-otp-send", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => Window(5, TimeSpan.FromMinutes(15))));
    options.AddPolicy("auth-otp-verify", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => Window(10, TimeSpan.FromMinutes(5))));
    options.AddPolicy("auth-register", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => Window(5, TimeSpan.FromHours(1))));
    options.AddPolicy("auth-refresh", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(ctx), _ => Window(30, TimeSpan.FromMinutes(1))));
});
builder.Services.AddHostedService<Core.WebApi.BackgroundServices.RefundEscalationService>();
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
    // Increase request timeout for video upload and HLS transcoding (10 minutes)
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});

// Forwarded headers (needed behind ALB/Nginx in Production)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Database migration and seeding (from old Program.cs)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
var context = services.GetRequiredService<ApplicationDbContext>();
            var configuration = services.GetRequiredService<IConfiguration>();
            // Only auto-migrate/seed when explicitly enabled (default false).
            // Production migrations should be run as a deliberate, separate deploy step.
            var runMigrations = configuration.GetValue<bool>("RunMigrationsOnStartup");
            if (context.Database.IsNpgsql() && runMigrations)
            {
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                context.Database.Migrate();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                await ApplicationDbContextSeed.SeedAsync(userManager, roleManager, configuration, context);

            }
            else
            {
                logger.LogInformation("RunMigrationsOnStartup is disabled (or provider is not Npgsql); " +
                                      "skipping automatic database migration and seeding.");
            }
            
            // Validate Stripe webhook secret — fail fast in production.
            // Without it, EventUtility.ConstructEvent verifies signatures against an
            // empty secret and every real webhook fails at runtime instead of at deploy.
            if (app.Environment.IsProduction())
            {
                var stripeWebhookSecret = configuration["Stripe:WebhookSecret"];
                // The committed appsettings.production.json carries only a documented
                // placeholder; the real value must be supplied via the Stripe__WebhookSecret
                // environment variable. Reject both missing and the un-overridden placeholder.
                if (string.IsNullOrWhiteSpace(stripeWebhookSecret) ||
                    stripeWebhookSecret.Contains("SET_VIA_ENVIRONMENT"))
                {
                    throw new InvalidOperationException(
                        "Stripe:WebhookSecret is not configured. Set 'Stripe__WebhookSecret' " +
                        "(or Stripe:WebhookSecret) before starting in production.");
                }
            }

            // Validate AWS region configuration
            var awsRegion = builder.Configuration["Aws:AwsRegion"];
            if (string.IsNullOrEmpty(awsRegion))
            {
                logger.LogWarning("AWS region not configured. Using default fallback (ap-south-1). " +
                                  "Please add 'AwsRegion' to appsettings for production use.");
            }
            else
            {
                logger.LogInformation("AWS Region configured: {AwsRegion}", awsRegion);
            }
            
            // Pre-load email logo into cache during startup
        try
        {
            var emailLogoService = services.GetRequiredService<IEmailLogoService>();
            var logoPreloaded = await emailLogoService.PreloadLogoAsync();
            if (logoPreloaded)
            {
                logger.LogInformation("Email logo successfully pre-loaded during startup.");
            }
            else
            {
                logger.LogWarning("Email logo failed to pre-load during startup. Emails may be sent without logo.");
            }
        }
        catch (Exception logoEx)
        {
            logger.LogError(logoEx, "Error pre-loading email logo during startup. Emails may be sent without logo.");
            // Don't throw - logo failure shouldn't prevent app from starting
        }
        
        logger.LogInformation("App started.");
    }
    catch (Exception e)
    {
        logger.LogError(e, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// Middleware pipeline (from Startup.Configure)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<UserEngagementMiddleware>();
app.UseMiddleware<SmartParameterValidationMiddleware>();

// if (app.Environment.IsDevelopment())
// {
//     app.UseHangfireDashboard("/jobs", new DashboardOptions()
//     {
//         Authorization = [new HangFireAuthorizationFilter()]
//     });
//     //app.UseBlockingDetection();
//     app.UseDeveloperExceptionPage();
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "PULR API v1");
//         c.InjectStylesheet("/swagger/ui/swagger-custom.css");
//         c.InjectJavascript("/swagger/ui/swagger-custom.js");
//         c.DefaultModelsExpandDepth(-1);
//         //c.EnablePersistAuthorization();
//     });
// }
// else
// {
//     // Enable Swagger for production ONLY when running locally (prevent exposure in live deployment)
//     // To use this locally, set the environment variable IS_LOCAL_RUN=true
//     var enableSwaggerInProduction = builder.Configuration.GetValue<bool>("EnableSwaggerInProduction", true);
//     var isLocalRun = string.Equals(Environment.GetEnvironmentVariable("IS_LOCAL_RUN"), "true", StringComparison.OrdinalIgnoreCase);
    
//     if (enableSwaggerInProduction && isLocalRun)
//     {
//         app.UseSwagger();
//         app.UseSwaggerUI(c =>
//         {
//             c.SwaggerEndpoint("/swagger/v1/swagger.json", "PULR API v1");
//             c.InjectStylesheet("/swagger/ui/swagger-custom.css");
//             c.InjectJavascript("/swagger/ui/swagger-custom.js");
//             c.DefaultModelsExpandDepth(-1);
//         });
//     }

//     // Secure Hangfire dashboard in non-Development environments
//     app.UseHangfireDashboard("/jobs", new DashboardOptions()
//     {
//         Authorization = [new HangFireAuthorizationFilter()]
//     });
// }

if (SwaggerConfiguration.IsEnabled(builder.Configuration))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PULR API v1");
        c.InjectStylesheet("/swagger/ui/swagger-custom.css");
        c.InjectJavascript("/swagger/ui/swagger-custom.js");
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseExceptionHandler("/errors");
app.UseStaticFiles();
// Respect proxy headers before any redirects/auth
app.UseForwardedHeaders();
// Only force HTTPS outside of Development to avoid redirecting IIS Express HTTP (56045) to a broken HTTPS port
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("_myAllowSpecificOrigins");
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    HangfireJobScheduler.ScheduleRecurringJobs(recurringJobManager);
}
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Endpoint mappings (recommended style in modern ASP.NET Core)
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
