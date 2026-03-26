using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace Core.Infrastructure.Swagger
{
    /// <summary>
    /// Hides dev-only Test endpoints from the Swagger document when not running in Development.
    /// Ensures delete-all-posts and delete-all-user-posts only appear in local dev Swagger.
    /// </summary>
    public class DevOnlyEndpointsDocumentFilter : IDocumentFilter
    {
        private static readonly HashSet<string> DevOnlyPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "/api/Test/delete-all-posts",
            "/api/Test/delete-all-user-posts"
        };

        private readonly IWebHostEnvironment _env;

        public DevOnlyEndpointsDocumentFilter(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            if (_env.IsDevelopment())
                return;

            if (document.Paths == null)
                return;

            foreach (var path in DevOnlyPaths)
            {
                document.Paths.Remove(path);
            }
        }
    }
}
