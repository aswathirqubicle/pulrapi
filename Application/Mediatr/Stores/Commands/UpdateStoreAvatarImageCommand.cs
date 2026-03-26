using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class UpdateStoreAvatarImageCommand : IRequest<string>
    {
        [Required] public string StoreUid { get; set; }

        [Required]
        [MaxFileSize(5 * 1024 * 1024)]
        [PulrFileValidation(new FileTypeEnum[] { FileTypeEnum.Image })]
        public IFormFile Image { get; set; }

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
                return await _storeService.UpdateStoreAvatarImage(request.StoreUid, request.Image,
                    request.ProfileImageType,
                    cancellationToken);

                /*Store store = await _dbContext.Stores.SingleOrDefaultAsync(s =>
                    s.Uid == request.StoreUid && s.User.Id == _currentUserService.GetUserId() && s.IsActive, cancellationToken);

                if (store == null)
                {
                    throw new BadRequestException($"Store with uid '{request.StoreUid}' doesnt exist.");
                }

                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                var fileConfig = new FileUploadConfigDto()
                {
                    FileName = request.Image.FileName,
                    BucketName = bucketName,
                    FolderPath = folderPath,
                    File = request.Image,
                    ImageWidth = request.ProfileImageType == ProfileImageTypeEnum.Avatar
                        ? Core.Application.Constants.PulrGlobalConfig.AvatarImage.Width                        : PulrGlobalConfig.BannerImage.Width,
                    ImageHeight = request.ProfileImageType == ProfileImageTypeEnum.Avatar
                        ? Core.Application.Constants.PulrGlobalConfig.AvatarImage.Height                        : PulrGlobalConfig.BannerImage.Height,
                };

                string path = null;

                if (request.ProfileImageType == ProfileImageTypeEnum.Avatar)
                {
                    if (store.ImageUrl != null)
                    {
                        fileConfig.OldFileName = store.ImageUrl.Substring(store.ImageUrl.LastIndexOf("/") + 1);
                        await _fileUploadService.Delete(fileConfig);
                    }

                    path = await _fileUploadService.UploadImage(fileConfig);
                    store.ImageUrl = path;
                }
                else
                {
                    if (store.BannerUrl != null)
                    {
                        fileConfig.OldFileName = store.BannerUrl.Substring(store.BannerUrl.LastIndexOf("/") + 1);
                        await _fileUploadService.Delete(fileConfig);
                    }

                    path = await _fileUploadService.UploadImage(fileConfig);
                    store.BannerUrl = path;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return path;*/
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}