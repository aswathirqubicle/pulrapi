using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Onboarding.Commands
{
    public class UpdateVibesCommand : IRequest<Unit>
    {
        public string[] VibeUids { get; set; }
    }

    public class UpdateVibesCommandHandler : IRequestHandler<UpdateVibesCommand, Unit>
    {
        private readonly ILogger<UpdateVibesCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UpdateVibesCommandHandler(ILogger<UpdateVibesCommandHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateVibesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate each vibe UID for security threats
                if (request.VibeUids != null)
                {
                    var safeUidAttribute = new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true);
                    
                    for (int i = 0; i < request.VibeUids.Length; i++)
                    {
                        var validationContext = new ValidationContext(new { VibeUid = request.VibeUids[i] }) { MemberName = $"VibeUids[{i}]" };
                        var validationResult = safeUidAttribute.GetValidationResult(request.VibeUids[i], validationContext);
                        
                        if (validationResult != ValidationResult.Success)
                        {
                            throw new Core.Application.Exceptions.ValidationException(validationResult.ErrorMessage);
                        }
                    }
                }

                var currentUser = await _currentUserService.GetUserAsync();
                
                // Remove existing vibes
                var existingVibes = await _dbContext.ProfileVibes
                    .Where(pv => pv.ProfileId == currentUser.Profile.Id)
                    .ToListAsync(cancellationToken);
                
                if (existingVibes.Any())
                {
                    _dbContext.ProfileVibes.RemoveRange(existingVibes);
                }

                // Add new vibes
                if (request.VibeUids != null && request.VibeUids.Any())
                {
                    var vibesToAdd = await _dbContext.Vibes
                        .Where(v => request.VibeUids.Contains(v.Uid) && v.IsActive)
                        .Select(v => new ProfileVibe
                        {
                            ProfileId = currentUser.Profile.Id,
                            VibeId = v.Id
                        })
                        .ToListAsync(cancellationToken);

                    _dbContext.ProfileVibes.AddRange(vibesToAdd);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
