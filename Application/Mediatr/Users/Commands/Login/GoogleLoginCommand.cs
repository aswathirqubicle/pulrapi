using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Models.Users;
using System;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class GoogleLoginCommand : IRequest<LoginResponse>
    {
        [Required]
        public string AccessToken { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PictureUrl { get; set; }
        public bool IsEmailVerified { get; set; }
        public DeviceDto Device { get; set; }
    }

    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResponse>
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(IUserService userService, INotificationService notificationService, ILogger<GoogleLoginCommandHandler> logger)
        {
            _userService = userService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<LoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var loginResponse = await _userService.LoginWithGoogleAsync(
                request.AccessToken,
                request.FirstName,
                request.LastName,
                request.PictureUrl,
                request.IsEmailVerified,
                request.Device
            );
            
            // Save push token if provided
            if (request.Device != null && !string.IsNullOrEmpty(request.Device.PushToken) && !string.IsNullOrEmpty(request.Device.DeviceIdentifier))
            {
                try
                {
                    await _notificationService.SavePushTokenAsync(loginResponse.Id, request.Device.PushToken, request.Device.DeviceIdentifier);
                    _logger.LogInformation("Push token saved for user {UserId} on device {DeviceId}", loginResponse.Id, request.Device.DeviceIdentifier);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save push token for user {UserId} on device {DeviceId}", loginResponse.Id, request.Device.DeviceIdentifier);
                    // Don't throw here as login should still succeed even if push token saving fails
                }
            }
            
            return loginResponse;
        }
    }
} 