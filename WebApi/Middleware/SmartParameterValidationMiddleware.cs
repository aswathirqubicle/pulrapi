using Core.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApi.Middleware;

public class SmartParameterValidationMiddleware
    (
    RequestDelegate next, 
    ILogger<SmartParameterValidationMiddleware> logger
    )
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<SmartParameterValidationMiddleware> _logger = logger;

    // Configure these arrays as needed
    private static readonly string[] SkipPaths =
    [
        "/swagger", "/health", "/metrics", "/_framework",
        //"/api/search", "/api/filter", "/api/users",
        //"/api/products","/api/orders","/api/bookmarks",
        // "/api/collections","/api/comments","/api/countries",
        // "/api/documents","/api/feed",
        // "/api/media-files","/api/messages","/api/notification",
        // "/api/onboarding","/api/posts","/api/profiles",
        // "/api/profile-settings","/api/status","/api/stories",
        // "/api/tags","/api/test","/api/users"
    ];

    private static readonly string[] MaliciousPatterns =
    {
        "<script", "</script>", "javascript:", "vbscript:",
        "' or ", "' OR ", "union select", "UNION SELECT",
        "drop table", "DROP TABLE", "exec(", "EXEC(","' OR 1=1--",
        "alert(", "eval(", "../", "..\\", "--", "/*"
    };

    // Pagination limits to prevent performance issues
    private const int MaxPageNumber = 10000; // Reasonable upper limit for page numbers
    private const int MaxPageSize = 100; // Consistent with PagingParamsRequest
    private const int MinPageNumber = 1;
    private const int MinPageSize = 1;

    // Common pagination parameter names (case-insensitive)
    private static readonly string[] PaginationParams = 
    {
        "pagenumber", "page", "pageno", "p",
        "pagesize", "size", "limit", "take"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Skip validation for allowed paths
        if (SkipPaths.Any(skip => path?.StartsWith(skip) == true))
        {
            await _next(context);
            return;
        }

        // Check query parameters for malicious patterns and pagination limits
        foreach (var param in context.Request.Query)
        {
            foreach (var value in param.Value)
            {
                // Check for malicious patterns
                if (ContainsMaliciousPattern(value))
                {
                    await BlockRequest(context, param.Key, value, "Malicious pattern detected");
                    return;
                }

                // Check pagination limits
                if (IsPaginationParameter(param.Key) && !IsValidPaginationValue(param.Key, value))
                {
                    await BlockRequest(context, param.Key, value, "Invalid pagination parameter");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ContainsMaliciousPattern(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        return MaliciousPatterns.Any(pattern =>
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPaginationParameter(string paramName)
    {
        if (string.IsNullOrEmpty(paramName)) return false;
        
        return PaginationParams.Any(paginationParam =>
            paramName.Equals(paginationParam, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidPaginationValue(string paramName, string value)
    {
        if (string.IsNullOrEmpty(value)) return true; // Allow empty values, let other validation handle it

        if (!int.TryParse(value, out int intValue)) return false;

        // Check if it's a page number parameter
        if (IsPageNumberParameter(paramName))
        {
            return intValue >= MinPageNumber && intValue <= MaxPageNumber;
        }

        // Check if it's a page size parameter
        if (IsPageSizeParameter(paramName))
        {
            return intValue >= MinPageSize && intValue <= MaxPageSize;
        }

        return true; // Not a pagination parameter, let it through
    }

    private static bool IsPageNumberParameter(string paramName)
    {
        return paramName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("page", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("pageno", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("p", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPageSizeParameter(string paramName)
    {
        return paramName.Equals("pagesize", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("size", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("limit", StringComparison.OrdinalIgnoreCase) ||
               paramName.Equals("take", StringComparison.OrdinalIgnoreCase);
    }

    private async Task BlockRequest(HttpContext context, string paramName, string paramValue, string reason = "Invalid request parameters")
    {
        _logger.LogWarning("Blocked request: {Path}?{Param}={Value} from {IP} - Reason: {Reason}",
            context.Request.Path, paramName, paramValue, context.Connection.RemoteIpAddress, reason);

        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        
        var errorMessage = reason switch
        {
            "Malicious pattern detected" => "Request contains potentially malicious content",
            "Invalid pagination parameter" => $"Invalid pagination parameter. PageNumber must be between {MinPageNumber}-{MaxPageNumber}, PageSize must be between {MinPageSize}-{MaxPageSize}",
            _ => "Invalid request parameters"
        };
        
        await context.Response.WriteAsync($"{{\"error\": \"{errorMessage}\"}}");
    }
}

// Extension method for easy registration
//public static class MiddlewareExtensions
//{
//    public static IApplicationBuilder UseSmartParameterValidation(this IApplicationBuilder app)
//    {
//        return app.UseMiddleware<SmartParameterValidationMiddleware>();
//    }
//}
