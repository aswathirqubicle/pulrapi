using System.ComponentModel.DataAnnotations;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.Commands.Update.AvatarImage;

public class UpdateStoreAvatarImageCommand : IRequest<string>
{
    [Required] public string? StoreUid { get; set; }

    [Required]
    [MaxFileSize(5 * 1024 * 1024)]
    [PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
    public IFormFile? Image { get; set; }

    [Required] public ProfileImageTypeEnum ProfileImageType { get; set; } = ProfileImageTypeEnum.Avatar;
}

public class UpdateStoreAvatarImageCommandHandler : IRequestHandler<UpdateStoreAvatarImageCommand, string>
{
    private readonly ILogger<UpdateStoreAvatarImageCommandHandler> _logger;
    private readonly IStoreService _storeService;

    public UpdateStoreAvatarImageCommandHandler(ILogger<UpdateStoreAvatarImageCommandHandler> logger,
        IStoreService storeService)
    {
        _logger = logger;
        _storeService = storeService;
    }

    public async Task<string> Handle(UpdateStoreAvatarImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _storeService.UpdateStoreAvatarImage(request.StoreUid, request.Image, request.ProfileImageType,
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error changing store avatar image");
            throw;
        }
    }
}