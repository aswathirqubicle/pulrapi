using MediatR;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Mediatr.Users.Commands.Register;
using Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

namespace Core.Application.Mediatr.Users.Commands.Register
{
    public class ConfirmEmailCommand : IRequest<Unit>
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }
    }

    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Unit>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ConfirmEmailCommandHandler> _logger;

        public ConfirmEmailCommandHandler(UserManager<User> userManager, ILogger<ConfirmEmailCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Unit> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(command.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Email confirmation failed: User with email {command.Email} not found");
                    throw new BadRequestException("Invalid email confirmation request");
                }

                var result = await _userManager.ConfirmEmailAsync(user, command.Token);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Email confirmation failed for user {user.Email}: {errors}");
                    throw new BadRequestException("Invalid email confirmation token");
                }

                // Update user's IsVerified flag
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"Email confirmed successfully for user {user.Email}");
                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming email for {command.Email}");
                throw;
            }
        }
    }
}
