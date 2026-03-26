using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Password;

namespace Core.Application.Mediatr.Users.Commands.Password
{
    public class PasswordResetRequestCommand : IRequest<Unit>
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }

    public class PasswordResetRequestCommandHandler : IRequestHandler<PasswordResetRequestCommand, Unit>
    {
        private readonly ILogger<PasswordResetRequestCommandHandler> _logger;
        private readonly IUserService _userService;

        public PasswordResetRequestCommandHandler(ILogger<PasswordResetRequestCommandHandler> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public async Task<Unit> Handle(PasswordResetRequestCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _userService.ManagePasswordResetRequest(request.Email);
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
