using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Security.Validation.Services;

namespace Core.Application.Behaviors
{
    /// <summary>
    /// Optimized validation behavior with caching and performance improvements
    /// </summary>
    public class OptimizedValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly OptimizedValidationService _validationService;

        public OptimizedValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            OptimizedValidationService validationService)
        {
            _validators = validators;
            _validationService = validationService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            // Use optimized validation service for bulk validation
            var validationResults = await Task.Run(() => ValidateRequest(request), cancellationToken);

            if (validationResults.Any())
            {
                var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
                if (failures.Any())
                {
                    throw new ValidationException(failures);
                }
            }

            return await next();
        }

        private List<FluentValidation.Results.ValidationResult> ValidateRequest(TRequest request)
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = new List<FluentValidation.Results.ValidationResult>();

            // Process validators in parallel for better performance
            var validationTasks = _validators.Select(validator => 
                Task.Run(() => validator.ValidateAsync(context))).ToArray();

            Task.WaitAll(validationTasks);

            foreach (var task in validationTasks)
            {
                validationResults.Add(task.Result);
            }

            return validationResults;
        }
    }
}
