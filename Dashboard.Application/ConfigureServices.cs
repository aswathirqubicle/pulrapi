using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Core.Application.Behaviors;

namespace Dashboard.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddDashboardApplication(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // Only register Swagger services in Development environment

            services.AddSwaggerGen(config =>
            {
                //use fully qualified object names
                config.CustomSchemaIds(x => x.FullName);
            });

            //services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                
                // Dashboard MediatR license configuration (disabled - dashboard not currently used)
                // if (configuration != null)
                // {
                //     var licenseKey = configuration["MediatR:LicenseKey"];
                //     if (!string.IsNullOrEmpty(licenseKey) && licenseKey != "YOUR_LICENSE_KEY_HERE")
                //     {
                //         cfg.LicenseKey = licenseKey;
                //     }
                // }
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
