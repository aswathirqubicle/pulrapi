using System.ComponentModel.DataAnnotations;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.Update.AvatarImage;

public class UpdateUserAvatarImageCommand : IRequest<string>
{
    [Required]
    public string? UserId { get; set; }

    [Required]
    [MaxFileSize(5 * 1024 * 1024)]
    [PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
    public IFormFile? Image { get; set; }
}

public class UpdateUserAvatarImageCommandHandler : IRequestHandler<UpdateUserAvatarImageCommand, string>
{
    private readonly ILogger<UpdateUserAvatarImageCommandHandler> _logger;
    private readonly IProfileService _profileService;
    private readonly IApplicationDbContext _dbContext;

    public UpdateUserAvatarImageCommandHandler(ILogger<UpdateUserAvatarImageCommandHandler> logger,
        IProfileService profileService, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _profileService = profileService;
        _dbContext = dbContext;
    }

    public async Task<string> Handle(UpdateUserAvatarImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);
            if (profile == null)
                throw new NotFoundException("Profile not found");

            return await _profileService.ProfileUpdateAvatarImage(profile, request.Image);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}