using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Profiles.Commands
{
    public class UnlinkExternalAccountCommand : IRequest<bool>
    {
        public string Provider { get; set; } // "Google" or "Apple"
    }

    public class UnlinkExternalAccountCommandHandler : IRequestHandler<UnlinkExternalAccountCommand, bool>
    {
        private readonly ILogger<UnlinkExternalAccountCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;

        public UnlinkExternalAccountCommandHandler(
            ILogger<UnlinkExternalAccountCommandHandler> logger,
            ICurrentUserService currentUserService,
            UserManager<User> userManager,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(UnlinkExternalAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("User not found");
                }

                // Get the user's external logins
                var logins = await _userManager.GetLoginsAsync(currentUser);
                var loginToRemove = logins.FirstOrDefault(l => l.LoginProvider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));

                if (loginToRemove == null)
                {
                    throw new InvalidOperationException($"No {request.Provider} account linked to this user");
                }

                // Check if user has a password set (required for unlinking)
                if (!await _userManager.HasPasswordAsync(currentUser))
                {
                    throw new InvalidOperationException("Cannot unlink external account. Please set a password first.");
                }

                // Remove the external login
                var result = await _userManager.RemoveLoginAsync(currentUser, loginToRemove.LoginProvider, loginToRemove.ProviderKey);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to unlink {request.Provider} account: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                _logger.LogInformation($"User {currentUser.Id} unlinked {request.Provider} account");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlinking {request.Provider} account");
                throw;
            }
        }
    }
} 