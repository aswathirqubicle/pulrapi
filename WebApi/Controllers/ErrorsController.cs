using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Helpers;
using Core.Application.Models;
using Core.Application.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : ControllerBase
    {
        [Route("errors")]
        public IActionResult ErrorDev(
        [FromServices] IWebHostEnvironment webHostEnvironment)
        {
            try
            {
                var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
                if (context == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "An error occurred but no exception details are available."
                    });
                }

                var exception = context.Error;
                if (exception == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "An error occurred but no exception was found."
                    });
                }

                var statusCode = ExceptionHelper.SetHttpStatusCodeBasedOnExceptionType(exception);
                Response.StatusCode = statusCode;

                // In non-development environments, return a minimal payload with only the message
                var isDevelopment = webHostEnvironment?.EnvironmentName == "Development";
                if (!isDevelopment)
                {
                    var safeMessage = GetSafeErrorMessage(exception, statusCode);
                    return StatusCode(Response.StatusCode, new { message = safeMessage });
                }

                var exceptionRes = new ExceptionResponseDto
                {
                    StatusCode = statusCode,
                    Message = GetSafeErrorMessage(exception, statusCode),
                    Details = exception.StackTrace
                };

                // Handle validation errors
                if (exception is Core.Application.Exceptions.ValidationException validationException)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    exceptionRes.StatusCode = StatusCodes.Status400BadRequest;
                    exceptionRes.Errors = new Dictionary<string, string[]>
                    {
                        { "File", new[] { validationException.Message ?? "File validation failed" } }
                    };
                }
                else if (statusCode == StatusCodes.Status422UnprocessableEntity)
                {
                    var validationEx = exception as dynamic;
                    if (validationEx?.Errors != null)
                    {
                        exceptionRes.Errors = validationEx.Errors;
                    }
                }

                return StatusCode(Response.StatusCode, exceptionRes);
            }
            catch (Exception ex)
            {
                // If something goes wrong in the error handler itself
                return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "An error occurred while processing the request",
                    Details = webHostEnvironment?.EnvironmentName == "Development" ? ex.ToString() : null
                });
            }
        }

        private string GetSafeErrorMessage(Exception exception, int statusCode)
        {
            // For JWT-related exceptions, return generic messages to avoid exposing internal details
            if (exception is SecurityTokenMalformedException)
            {
                return "Invalid token format";
            }
            if (exception is SecurityTokenExpiredException)
            {
                return "Token has expired";
            }
            if (exception is SecurityTokenNotYetValidException)
            {
                return "Token is not yet valid";
            }
            if (exception is SecurityTokenException)
            {
                return "Invalid token";
            }
            
            // For 401 errors, use generic message
            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                return "Unauthorized";
            }
            
            // For NotFoundException in password reset context, return generic message
            if (exception is NotFoundException && exception.Message.Contains("User not found"))
            {
                return "Invalid email format";
            }
            
            // For other exceptions, return the original message or a generic one
            return exception.Message ?? "An error occurred";
        }
    }
}
