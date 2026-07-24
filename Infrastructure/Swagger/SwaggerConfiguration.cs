using Microsoft.Extensions.Configuration;

namespace Core.Infrastructure.Swagger
{
    public static class SwaggerConfiguration
    {
        public static bool IsEnabled(IConfiguration configuration)
        {
            var enabled = configuration.GetValue<bool>("Swagger:Enabled");
            var environment = configuration["Swagger:Environment"];
            var isProductionSwaggerEnvironment = string.Equals(environment, "Production", System.StringComparison.OrdinalIgnoreCase);

            return enabled && !isProductionSwaggerEnvironment;
        }
    }
}
