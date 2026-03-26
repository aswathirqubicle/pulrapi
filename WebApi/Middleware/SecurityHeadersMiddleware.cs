using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebApi.Middleware
{
    /// <summary>
    /// Middleware to add security headers to all HTTP responses
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        // Development tool paths that need less restrictive security headers
        private static readonly string[] DevToolPaths = { "/swagger", "/jobs", "/hangfire" };

        // Security header constants
        private const string ServerHeaderValue = "WebServer";
        private const string HstsHeaderValue = "max-age=31536000; includeSubDomains; preload";
        private const string ApiCspValue = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; object-src 'none'; form-action 'none'; upgrade-insecure-requests";
        private const string DevCspValue = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self'; frame-ancestors 'self'";

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Anonymize server header to prevent software version disclosure
            AnonymizeServerHeader(context);

            // Apply appropriate security headers based on request path
            if (IsDevelopmentTool(context.Request.Path))
            {
                ApplyDevelopmentToolHeaders(context);
            }
            else
            {
                ApplyProductionSecurityHeaders(context);
            }

            await _next(context);
        }

        private static void AnonymizeServerHeader(HttpContext context)
        {
            context.Response.Headers.Remove("Server");
            context.Response.Headers["Server"] = ServerHeaderValue;
        }

        private static bool IsDevelopmentTool(PathString path)
        {
            var pathValue = path.Value?.ToLower();
            if (string.IsNullOrEmpty(pathValue))
                return false;

            foreach (var devPath in DevToolPaths)
            {
                if (pathValue.StartsWith(devPath))
                    return true;
            }
            return false;
        }

        private static void ApplyProductionSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Check if this is an HLS file (playlist or segment)
            bool isHlsFile = path.EndsWith(".m3u8") || path.EndsWith(".ts");

            // Core security headers (apply to all except HLS files)
            if (!isHlsFile)
            {
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                headers["Content-Security-Policy"] = ApiCspValue;

                // Cache control headers to prevent sensitive data caching
                headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "0";
            }
            else
            {
                // HLS-specific headers for optimal streaming on iOS
                // iOS AVPlayer requires caching for smooth HLS playback
                headers["Cache-Control"] = "public, max-age=3600"; // Cache for 1 hour
                headers["Access-Control-Allow-Origin"] = "*"; // CORS for HLS
                headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS";
                headers["Access-Control-Allow-Headers"] = "Range";
                headers["Accept-Ranges"] = "bytes"; // Enable range requests for segments
                
                // Set proper Content-Type for HLS files
                if (path.EndsWith(".m3u8"))
                {
                    headers["Content-Type"] = "application/vnd.apple.mpegurl";
                }
                else if (path.EndsWith(".ts"))
                {
                    headers["Content-Type"] = "video/mp2t";
                }
            }

            // HSTS - only for HTTPS connections
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = HstsHeaderValue;
            }
        }

        private static void ApplyDevelopmentToolHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Basic security headers for development tools
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Content-Security-Policy"] = DevCspValue;
        }
    }
}


