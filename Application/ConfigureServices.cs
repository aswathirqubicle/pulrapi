using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Core.Application.Behaviors;
using Core.Application.Models.Orders;

namespace Core.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration = null)
        {
            // Register OrderSettings from configuration
            if (configuration != null)
            {
                services.Configure<OrderSettings>(configuration.GetSection("OrderSettings"));
            }
            else
            {
                services.Configure<OrderSettings>(options => { });
            }

            // Only register Swagger services in Development environment
            if (configuration != null && configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
            {
                services.AddSwaggerGen(config =>
                {
                    //use fully qualified object names
                    config.CustomSchemaIds(x => x.FullName);
                });
            }

            //services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                
                // Add license key if configuration is available
                if (configuration != null)
                {
                    var licenseKey = configuration["MediatR:LicenseKey"];
                    if (!string.IsNullOrEmpty(licenseKey) && licenseKey != "YOUR_LICENSE_KEY_HERE")
                    {
                        cfg.LicenseKey = licenseKey;
                    }
                }
            });
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));

            return services;
        }
    }
}
