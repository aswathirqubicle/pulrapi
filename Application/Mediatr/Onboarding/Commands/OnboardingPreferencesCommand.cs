using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Application.Security.Validation.Attributes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;

namespace Core.Application.Mediatr.Onboarding.Commands
{
    public class OnboardingPreferencesCommand : IRequest <Unit>
    {
        public string[] Preferences { get; set; }
    }

    public class OnboardingPreferencesCommandHandler : IRequestHandler<OnboardingPreferencesCommand,Unit>
    {
        private readonly ILogger<OnboardingPreferencesCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public OnboardingPreferencesCommandHandler(ILogger<OnboardingPreferencesCommandHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(OnboardingPreferencesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate each preference for security threats
                if (request.Preferences != null)
                {
                    var safePreferenceAttribute = new SafePreferenceAttribute(allowNullValue: false, maxLength: 100, minLength: 1);
                    
                    for (int i = 0; i < request.Preferences.Length; i++)
                    {
                        var validationContext = new ValidationContext(new { Preference = request.Preferences[i] }) { MemberName = $"Preferences[{i}]" };
                        var validationResult = safePreferenceAttribute.GetValidationResult(request.Preferences[i], validationContext);
                        
                        if (validationResult != ValidationResult.Success)
                        {
                            throw new Core.Application.Exceptions.ValidationException(validationResult.ErrorMessage);
                        }
                    }
                }

                var currentUser = await _currentUserService.GetUserAsync();
                var existing = _dbContext.ProfileOnboardingPreferences.Where(p => p.ProfileId == currentUser.Profile.Id).ToList();
                if (existing.Any())
                {
                    _dbContext.ProfileOnboardingPreferences.RemoveRange(existing);
                }

                // Get valid preferences from database to prevent SQL injection
                var validPreferences = await _dbContext.OnboardingPreferences
                    .Where(e => e.IsActive && request.Preferences.Contains(e.Key))
                    .ToListAsync(cancellationToken);

                // Validate that all provided preferences exist in the database
                var invalidPreferences = request.Preferences.Except(validPreferences.Select(p => p.Key)).ToList();
                if (invalidPreferences.Any())
                {
                    throw new BadRequestException($"Invalid preferences: {string.Join(", ", invalidPreferences)}");
                }

                var preferencesToAdd = validPreferences.Select(e =>
                    new ProfileOnboardingPreference()
                    {
                        OnboardingPreferenceId = e.Id,
                        ProfileId = currentUser.Profile.Id
                    }).ToList();

                _dbContext.ProfileOnboardingPreferences.AddRange(preferencesToAdd);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }
    }
}
