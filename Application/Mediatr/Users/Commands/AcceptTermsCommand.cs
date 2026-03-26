using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using Core.Application.Interfaces;

namespace Core.Application.Mediatr.Users.Commands
{
    public class AcceptTermsCommand : IRequest<bool>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class AcceptTermsCommandHandler : IRequestHandler<AcceptTermsCommand, bool>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AcceptTermsCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public AcceptTermsCommandHandler(UserManager<User> userManager, ILogger<AcceptTermsCommandHandler> logger, ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUserService.IsUserLoggedIn())
                {
                    throw new NotAuthenticatedException("Authentication required");
                }

                var user = await _currentUserService.GetUserAsync(true);

                user.TermsAccepted = true;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning($"Failed to update terms acceptance for user {user.Email}: {errors}");
                    throw new BadRequestException("Failed to update terms acceptance");
                }

                _logger.LogInformation($"Terms accepted successfully for user {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accepting terms for {request.Email}");
                throw;
            }
        }
    }

    public class AcceptTermsCommandValidator : AbstractValidator<AcceptTermsCommand>
    {
        public AcceptTermsCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .MaximumLength(254).WithMessage("Email is too long")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }
} 