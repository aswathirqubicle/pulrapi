using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Users.NotificationHandlers
{
    public class AssignRoleNotificationHandler : INotificationHandler<StoreCreatedEvent>
    {
        private readonly ILogger<AssignRoleNotificationHandler> _logger;
        private readonly IUserService _userService;

        public AssignRoleNotificationHandler(ILogger<AssignRoleNotificationHandler> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public async Task Handle(StoreCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _userService.AssignRole(PulrRoles.StoreOwner);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
