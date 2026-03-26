using Core.Application;
using Core.Application.Hubs;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Security;
using Core.Domain.Entities;
using Core.Infrastructure;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Services.Cron;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new UploadMediaFileDtoModelBinderProvider());
});
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
        if (context.Database.IsNpgsql())
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            context.Database.Migrate();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            await ApplicationDbContextSeed.SeedAsync(userManager, roleManager, configuration, context);
            
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

if (app.Environment.IsDevelopment())
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
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Endpoint mappings (recommended style in modern ASP.NET Core)
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
