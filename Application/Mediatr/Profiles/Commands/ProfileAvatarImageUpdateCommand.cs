using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
//using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Profiles.Commands;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using Core.Application.Exceptions;

namespace Core.Application.Mediatr.Profiles.Commands
{
    public class ProfileAvatarImageUpdateCommand : IRequest<string>
    {
        [MaxFileSize(5 * 1024 * 1024)]
        [PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
        public IFormFile? Image { get; set; }
        
        public bool RemoveImage { get; set; } = false;
    }

    public class ProfileAvatarImageUpdateCommandHandler : IRequestHandler<ProfileAvatarImageUpdateCommand, string>
    {
        private readonly ILogger<ProfileAvatarImageUpdateCommandHandler> _logger;
        private readonly IProfileService _profileService;
        private readonly ICurrentUserService _currentUserService;

        public ProfileAvatarImageUpdateCommandHandler(ILogger<ProfileAvatarImageUpdateCommandHandler> logger, IProfileService profileService, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _profileService = profileService;
            _currentUserService = currentUserService;
        }
        public async Task<string> Handle(ProfileAvatarImageUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync();
                
                // Check if we need to remove the image (when RemoveImage is true)
                if (request.RemoveImage)
                {
                    var imagePath = await _profileService.ProfileUpdateAvatarImage(user.Profile, null);
                    return imagePath;
                }
                
                // If no Image is provided and RemoveImage is false, throw validation error
                if (request.Image == null)
                {
                    throw new ValidationException("Either Image file or RemoveImage flag must be provided.");
                }
                
                // Upload the new image
                var newImagePath = await _profileService.ProfileUpdateAvatarImage(user.Profile, request.Image);
                return newImagePath;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
