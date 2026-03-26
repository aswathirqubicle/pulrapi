using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Models.Users;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Users.Commands.Password
{
    public class ChangePasswordCommand : IRequest <Unit>
    {
        [Required]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password and password confirmation do not match.")]
        public string PasswordConfirmation { get; set; }
    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand,Unit>
    {
        private readonly ILogger<ChangePasswordCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public ChangePasswordCommandHandler(
            ILogger<ChangePasswordCommandHandler> logger,
            ICurrentUserService currentUserService,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
        }
        public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var model = _mapper.Map<PasswordChangeDto>(request);
                var user = await _currentUserService.GetUserAsync();
                if (user == null) { throw new ForbiddenException("User doesnt exist."); }

                if (model.OldPassword == model.NewPassword)
                {
                    throw new Exception("The new password must be different from the old password.");
                }

                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    throw new ForbiddenException(result.Errors.Select(e => e.ToString()).Aggregate((a, b) => a + ", " + b));
                }
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
