using Core.Application.Exceptions;
using Core.Application.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        public ExceptionHandlingMiddleware(RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var path = context.Request.Path.Value;

                // Check for dangerous patterns
                if (ContainsDangerousPatterns(path))
                {
                    _logger.LogWarning("Blocked malicious request: {Path} from {IP}",
                        path, context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid request format");
                    return;
                }

                await _next(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                await HandleExceptionAsync(context, e);
            }
        }

        private bool ContainsDangerousPatterns(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            var dangerousPatterns = new[]
            {
            "<script", "</script>", "javascript:", "vbscript:",
            "../", "..\\", "%2e%2e", "%2f", "%5c",
            "' or ", "' OR ", "union select", "UNION SELECT",
            "drop table", "DROP TABLE", "exec(", "EXEC("
        };

            return dangerousPatterns.Any(pattern =>
                input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        private static async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
        {
            var statusCode = GetStatusCode(exception);
            var isDevelopment = httpContext.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName == "Development";
            
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = statusCode;

            if (exception is ValidationException vex)
            {
                // Return a minimal payload for validation errors with only message
                var payload = new
                {
                    message = vex.Message
                };
                await httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }

            var response = new
            {
                title = GetTitle(exception),
                status = statusCode,
                detail = GetSafeErrorMessage(exception, isDevelopment),
                errors = GetErrors(exception)
            };
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        private static int GetStatusCode(Exception exception)
        {
            return ExceptionHelper.SetHttpStatusCodeBasedOnExceptionType(exception);
        }
        private static string GetTitle(Exception exception) =>
            exception switch
            {
                ApplicationException applicationException => applicationException.Source,
                _ => "Server Error"
            };
        private static IReadOnlyDictionary<string, string[]> GetErrors(Exception exception)
        {
            IReadOnlyDictionary<string, string[]> errors = null;
            if (exception is ValidationException validationException)
            {
                errors = (IReadOnlyDictionary<string, string[]>)validationException.Errors;
            }
            return errors;
        }

        private static string GetSafeErrorMessage(Exception exception, bool isDevelopment)
        {
            if (isDevelopment)
            {
                return exception.Message;
            }

            // Return generic error messages for production to avoid information disclosure
            return exception switch
            {
                ValidationException => "Invalid request data",
                NotFoundException => "The requested resource was not found",
                NotAuthenticatedException => "Authentication required",
                ForbiddenException => "Access denied",
                BadRequestException => "Invalid request",
                SecurityTokenMalformedException => "Invalid token format",
                SecurityTokenException => "Invalid token",
                _ => "An error occurred while processing your request"
            };
        }
    }
}