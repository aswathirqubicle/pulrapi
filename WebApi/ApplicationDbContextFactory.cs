using System;
using System.IO;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebApi
{
    /// <summary>
    /// Design-time factory used ONLY by EF Core tooling (migrations / database
    /// update). It builds the DbContext directly from configuration so the tooling
    /// does not have to start the full web host (whose startup connects to external
    /// services). Has no effect at runtime.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Pulr");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Design-time connection string not found. Set ConnectionStrings:Pulr in appsettings " +
                    "or the ConnectionStrings__Pulr environment variable.");
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .Options;

            // Interceptor and mediator are not exercised during design-time scaffolding.
            return new ApplicationDbContext(options, null, null);
        }
    }
}
