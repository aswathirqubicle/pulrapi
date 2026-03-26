using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Users.Commands.Register
{
    public class RegisterCommand : IRequest<Unit>
    {
        [Required]
        [PulrNameValidation]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        [PulrUsernameValidation]
        public string Username { get; set; }

        [StrongPassword]
        public string Password { get; set; }

        public string CountryUid { get; set; }

        public GenderEnum? Gender { get; set; }

        public string UserType { get; set; }

        [Required]
        public bool TermsAccepted { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }

        public bool IsSocialLogin { get; set; } // true for Google/Apple registration
        public string CommunicationMail { get; set; }

        public DeviceDto Device { get; set; }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Unit>
    {
        private readonly ILogger<RegisterCommandHandler> logger;
        private readonly IUserService userService;
        private readonly IProfileService profileService;
        private readonly INotificationService notificationService;
        private readonly IApplicationDbContext dbContext;

        public RegisterCommandHandler(ILogger<RegisterCommandHandler> logger,
            IUserService userService,
            IProfileService profileService,
            INotificationService notificationService,
            IApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.userService = userService;
            this.profileService = profileService;
            this.notificationService = notificationService;
            this.dbContext = dbContext;
        }

        public async Task<Unit> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (!request.TermsAccepted)
            {
                throw new BadRequestException("You must accept the terms and conditions to register");
            }

            var registerDto = new UserRegisterDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Username = request.Username,
                Password = request.Password,
                CountryUid = request.CountryUid,
                Gender = request.Gender,
                TermsAccepted = request.TermsAccepted,
                DateOfBirth = request.DateOfBirth,
                UserType = request.UserType,
                IsSocialLogin = request.IsSocialLogin,
                CommunicationMail = request.CommunicationMail
            };

            var response = await userService.RegisterAsync(registerDto);
            if (!response.IsSuccess)
            {
                throw new BadRequestException(response.Message);
            }

            await profileService.Create(response.User, request.Gender, request.UserType);

            // Only save login activity if:
            // - Normal registration
            // - Social registration and user is not new
            if (
                (!request.IsSocialLogin && response.User != null && request.Device != null) ||
                (request.IsSocialLogin && !response.IsNewUser && response.User != null && request.Device != null)
            )
            {
                await userService.SaveLoginActivityAsync(
                    response.User.Id,
                    request.Device.Brand,
                    request.Device.ModelName,
                    request.Device.OsVersion,
                    request.Device.DeviceIdentifier,
                    request.Device.AppVersion,
                    "Logged in"
                );
            }

            // Save push token if provided and user was created/authenticated
            if (response.User != null && request.Device != null && !string.IsNullOrEmpty(request.Device.PushToken) && !string.IsNullOrEmpty(request.Device.DeviceIdentifier))
            {
                try
                {
                    await notificationService.SavePushTokenAsync(response.User.Id, request.Device.PushToken, request.Device.DeviceIdentifier);
                    logger.LogInformation("Push token saved for user {UserId} on device {DeviceId} during registration", response.User.Id, request.Device.DeviceIdentifier);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to save push token for user {UserId} on device {DeviceId} during registration", response.User.Id, request.Device.DeviceIdentifier);
                    // Don't throw here as registration should still succeed even if push token saving fails
                }
            }

            return Unit.Value;
        }
    }
}
