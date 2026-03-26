using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Core.Application.Interfaces;
using System;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Middleware
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public TokenBlacklistMiddleware(RequestDelegate next, ITokenBlacklistService tokenBlacklistService)
        {
            _next = next;
            _tokenBlacklistService = tokenBlacklistService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (token != null)
                {
                    // Validate token format before attempting to parse
                    if (!IsValidJwtFormat(token))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Invalid token format" });
                        return;
                    }

                    var jti = Core.Infrastructure.Services.Users.UserService.GetJtiFromToken(token);
                    
                    // If jti is null, it means the token is malformed
                    if (jti == null)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Invalid token format" });
                        return;
                    }
                    
                    if (!string.IsNullOrEmpty(jti) && await _tokenBlacklistService.IsTokenBlacklistedAsync(jti))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Token has been invalidated" });
                        return;
                    }
                }

                await _next(context);
            }
            catch (SecurityTokenMalformedException)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid token format" });
                return;
            }
            catch (SecurityTokenException)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
                return;
            }
            catch
            {
                // Re-throw non-token exceptions so global handlers (e.g., 403 Forbidden) can surface proper messages
                throw;
            }
        }

        /// <summary>
        /// Validates if the token has the basic JWT format (header.payload.signature)
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <returns>True if the token has valid JWT format, false otherwise</returns>
        private static bool IsValidJwtFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // JWT should have exactly 2 dots separating 3 parts
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            // Basic validation: each part should not be empty and contain only valid base64url characters
            // Base64url uses A-Z, a-z, 0-9, -, and _ (no padding)
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    return false;
                
                // Check for obviously invalid characters (not base64url)
                if (part.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_'))
                    return false;
            }

            return true;
        }
    }
} 