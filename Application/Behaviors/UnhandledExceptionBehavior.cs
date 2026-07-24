using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Mediatr.Users.Commands.Register;
using System.Collections.Generic;
using Core.Application.Exceptions;

namespace Core.Application.Behaviors
{
    public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TRequest> _logger;
        private readonly List<string> _forbiddenRequestsForLog = new List<string>() { 
            nameof(LoginCommand), 
            nameof(RegisterCommand), 
            nameof(ChangePasswordFromEmailCommand), 
            nameof(ChangePasswordCommand), 
            // TODO FIX:
            "DashboardLoginCommand"
            };

        public UnhandledExceptionBehaviour(ILogger<TRequest> logger)
        {
            _logger = logger;
        }
        
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                var requestName = typeof(TRequest).Name;

                bool skipBody = false;

                if (_forbiddenRequestsForLog.Contains(requestName))
                {
                    skipBody = true;
                }

                object req;
                if (skipBody == true)
                {
                    req = new { sensitiveData = true };
                }
                else
                {
                    req = request;
                }

                _logger.LogError(ex, "PulrApi Request: Unhandled Exception for Request {Name} {@Request}", requestName, req);

                // Preserve known exception types instead of converting them to generic exceptions
                if (ex is BadRequestException || ex is NotAuthenticatedException || ex is ForbiddenException || ex is NotFoundException || ex is ValidationException)
                {
                    throw; // Re-throw the original exception to preserve its type and status code
                }
                
                // Do not leak the original/inner exception text in the thrown message.
                // The original exception is preserved as InnerException for server-side logging only.
                throw new Exception("An unexpected error occurred.", ex);
            }
        }
    }
}
