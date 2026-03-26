using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Models.Users;
using Core.Application.Mappings;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponse>, IMapFrom<LoginDto>
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public bool IsEmail { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public DeviceDto Device { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<LoginCommand, LoginDto>();
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public LoginCommandHandler(ILogger<LoginCommandHandler> logger, IUserService userService, INotificationService notificationService, IMapper mapper)
        {
            _logger = logger;
            _userService = userService;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var loginResponse = await _userService.LoginAsync(_mapper.Map<LoginDto>(request));
                
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
            catch (Exception e) 
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
