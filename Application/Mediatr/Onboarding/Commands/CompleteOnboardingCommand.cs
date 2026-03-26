using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Commands
{
    public class CompleteOnboardingCommand : IRequest <Unit>
    {
        public string[] VibeUids { get; set; }
    }

    public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand,Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public CompleteOnboardingCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();

                // Validate that vibe IDs are provided
                if (request.VibeUids == null || !request.VibeUids.Any())
                {
                    throw new ValidationException("Please select your vibes before completing onboarding");
                }

                // Remove existing vibes for this user
                var existingVibes = await _dbContext.ProfileVibes
                    .Where(pv => pv.ProfileId == currentUser.Profile.Id)
                    .ToListAsync(cancellationToken);
                
                if (existingVibes.Any())
                {
                    _dbContext.ProfileVibes.RemoveRange(existingVibes);
                }

                // Get vibes with their names
                var selectedVibes = await _dbContext.Vibes
                    .Where(v => request.VibeUids.Contains(v.Uid) && v.IsActive)
                    .ToListAsync(cancellationToken);

                if (!selectedVibes.Any())
                {
                    throw new ValidationException("Invalid vibe selection");
                }

                // Add new vibes based on provided IDs
                var vibesToAdd = selectedVibes.Select(v => new ProfileVibe
                {
                    ProfileId = currentUser.Profile.Id,
                    VibeId = v.Id
                }).ToList();

                _dbContext.ProfileVibes.AddRange(vibesToAdd);

                // Remove existing onboarding preferences
                var existingPreferences = await _dbContext.ProfileOnboardingPreferences
                    .Where(p => p.ProfileId == currentUser.Profile.Id)
                    .ToListAsync(cancellationToken);
                
                if (existingPreferences.Any())
                {
                    _dbContext.ProfileOnboardingPreferences.RemoveRange(existingPreferences);
                }

                // Get or create onboarding preferences for each vibe name
                foreach (var vibe in selectedVibes)
                {
                    var onboardingPreference = await _dbContext.OnboardingPreferences
                        .FirstOrDefaultAsync(op => op.Key == vibe.Name, cancellationToken);

                    if (onboardingPreference == null)
                    {
                        // Create new onboarding preference if it doesn't exist
                        onboardingPreference = new OnboardingPreference
                        {
                            Key = vibe.Name,
                            Name = vibe.Name,
                            IsActive = true
                        };
                        _dbContext.OnboardingPreferences.Add(onboardingPreference);
                        await _dbContext.SaveChangesAsync(cancellationToken); // Save to get the ID
                    }

                    // Create profile onboarding preference
                    var profileOnboardingPreference = new ProfileOnboardingPreference
                    {
                        ProfileId = currentUser.Profile.Id,
                        OnboardingPreferenceId = onboardingPreference.Id
                    };
                    _dbContext.ProfileOnboardingPreferences.Add(profileOnboardingPreference);
                }

                // Mark the onboarding as completed
                if (!currentUser.IsVerified)
                {
                    currentUser.IsVerified = true;
                    _dbContext.Users.Update(currentUser);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            }
            catch (Exception e)
            {
                //log the error
                throw new Exception("An error occurred while completing onboarding", e);
            }
        }
    }
}
