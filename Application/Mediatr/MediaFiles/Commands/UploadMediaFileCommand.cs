using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Helpers;
using Core.Application.Interfaces; // For other services (ICurrentUserService, IApplicationDbContext, etc.)
using Pulr.Contracts.Interfaces; // For cross-service interfaces (IVideoProcessingService)
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Hangfire;

namespace Core.Application.Mediatr.MediaFiles.Commands
{
    public class UploadMediaFileCommand : IRequest<List<MediaFileDetailsResponse>>
    {
        [Required, MaxFileSize(10 * 1024 * 1024), PulrFileValidation]
        public List<IFormFile> Files { get; set; }
        public string Type { get; set; }
        public bool MuteVideo { get; set; }
        public int? CropX { get; set; }
        public int? CropY { get; set; }
        public int? CropWidth { get; set; }
        public int? CropHeight { get; set; }
        public string FilterType { get; set; }
    }

    public class AddMediaFileToPostCommandHandler : IRequestHandler<UploadMediaFileCommand, List<MediaFileDetailsResponse>>
    {
        private readonly ILogger<AddMediaFileToPostCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _dbContext;
        private readonly IFileUploadService _fileUploadService;
        private readonly IVideoTranscodingService _videoTranscodingService;
        private readonly Hangfire.IBackgroundJobClient _videoJobClient;

        public AddMediaFileToPostCommandHandler(ILogger<AddMediaFileToPostCommandHandler> logger,
            IMapper mapper, 
            ICurrentUserService currentUserService, 
            IConfiguration configuration, 
            IApplicationDbContext dbContext, 
            IFileUploadService fileUploadService,
            IVideoTranscodingService videoTranscodingService,
            Hangfire.IBackgroundJobClient videoJobClient)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _dbContext = dbContext;
            _fileUploadService = fileUploadService;
            _videoTranscodingService = videoTranscodingService;
            _videoJobClient = videoJobClient;
        }
        
        public async Task<List<MediaFileDetailsResponse>> Handle(UploadMediaFileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync(true);
                var response = new List<MediaFileDetailsResponse>();

                string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string folderPath = request.Type == "Thumbnail" ? "thumnail/post" : _configuration[AwsLocationNames.PublicUploadFolder];

                foreach (var file in request.Files)
                {
                    var fileTypeInfo = FileHelper.CheckFile(file);
                    var fileConfig = new FileUploadConfigDto()
                    {
                        FileName = fileTypeInfo.Name,
                        BucketName = bucketName,
                        FolderPath = folderPath,
                        File = file,
                        ImageWidth = PulrGlobalConfig.PostImage.Width,
                        ImageHeight = PulrGlobalConfig.PostImage.Height,
                        FilterType = request.FilterType
                    };

                    MediaFile mediaFile;

                    if (fileTypeInfo.FileType == FileTypeEnum.Image)
                    {
                        // Handle image upload (existing logic)
                        var imageUrl = await _fileUploadService.UploadImage(fileConfig);
                        
                        mediaFile = new MediaFile()
                        {
                            Priority = 0,
                            MediaFileType = MediaFileTypeEnum.Image,
                            Url = imageUrl,
                            Uid = Guid.NewGuid().ToString(),
                            FilterType = request.FilterType
                        };
                    }
                    else // Video
                    {
                        // Upload original video immediately (fast, no timeout)
                        var videoUrl = await _fileUploadService.UploadVideo(fileConfig);

                        mediaFile = new MediaFile()
                        {
                            Priority = 0,
                            MediaFileType = MediaFileTypeEnum.Video,
                            Url = videoUrl, // Original video URL
                            OriginalUrl = videoUrl,
                            IsHlsProcessed = false, // Will be updated by background job
                            HlsBasePath = null,
                            VideoDurationSeconds = null,
                            AvailableQualities = null,
                            IsMuted = request.MuteVideo,
                            CropX = request.CropX,
                            CropY = request.CropY,
                            CropWidth = request.CropWidth,
                            CropHeight = request.CropHeight,
                            Uid = Guid.NewGuid().ToString(),
                            FilterType = request.FilterType
                        };

                        _logger.LogInformation("Video uploaded: Url={Url}. HLS transcoding will be queued for background processing.", videoUrl);
                    }

                    _dbContext.MediaFiles.Add(mediaFile);
                    response.Add(_mapper.Map<MediaFileDetailsResponse>(mediaFile));
                }             

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                // Queue background HLS transcoding jobs for videos (only if not a thumbnail type, though thumbnails are usually images)
                if (request.Type != "Thumbnail")
                {
                    foreach (var mediaFileResponse in response.Where(r => r.FileType == "Video"))
                    {
                        var mediaFileEntity = await _dbContext.MediaFiles
                            .FirstOrDefaultAsync(mf => mf.Uid == mediaFileResponse.Uid, CancellationToken.None);
                        
                        if (mediaFileEntity != null && !mediaFileEntity.IsHlsProcessed)
                        {
                            // Queue background job for HLS transcoding (to 'cron' schema for PulrWorker)
                            // Use fully qualified name to avoid ambiguity with Core.Application.Interfaces.IVideoProcessingService
                            _videoJobClient.Enqueue<Pulr.Contracts.Interfaces.IVideoProcessingService>(
                                service => service.ProcessHlsTranscodingAsync(mediaFileEntity.Id));
                            
                            _logger.LogInformation("Queued HLS transcoding job for MediaFile {MediaFileId} to 'cron' schema", mediaFileEntity.Id);
                        }
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }

}
