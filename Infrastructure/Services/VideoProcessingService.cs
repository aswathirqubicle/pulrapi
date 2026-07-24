using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.VideoTranscoding;
using Core.Application.Constants;
using Core.Application.Helpers;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IVideoTranscodingService _transcodingService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(
            IApplicationDbContext dbContext,
            IVideoTranscodingService transcodingService,
            IFileUploadService fileUploadService,
            IConfiguration configuration,
            ILogger<VideoProcessingService> logger)
        {
            _dbContext = dbContext;
            _transcodingService = transcodingService;
            _fileUploadService = fileUploadService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessHlsTranscodingAsync(int mediaFileId)
        {
            try
            {
                _logger.LogInformation("Starting HLS transcoding for MediaFile ID: {MediaFileId}", mediaFileId);

                var mediaFile = await _dbContext.MediaFiles
                    .FirstOrDefaultAsync(mf => mf.Id == mediaFileId);

                if (mediaFile == null)
                {
                    _logger.LogError("MediaFile with ID {MediaFileId} not found", mediaFileId);
                    return;
                }

                if (mediaFile.IsHlsProcessed)
                {
                    _logger.LogInformation("MediaFile {MediaFileId} already processed", mediaFileId);
                    return;
                }

                // Download the original video from S3 to temp location
                var tempInputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
                var tempOutputDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                System.IO.Directory.CreateDirectory(tempOutputDir);

                try
                {
                    // Download video from S3
                    _logger.LogInformation("MediaFile OriginalUrl: {OriginalUrl}", mediaFile.OriginalUrl);
                    await DownloadVideoFromS3(mediaFile.OriginalUrl, tempInputPath);

                    // Detect input video dimensions for orientation-aware transcoding (Instagram-style)
                    var (inputWidth, inputHeight) = await _transcodingService.GetVideoDimensionsAsync(tempInputPath);
                    var isPortrait = inputWidth > 0 && inputHeight > 0 && inputHeight > inputWidth;

                    // Clamp and apply crop params if all four are provided
                    int? cropX = null, cropY = null, cropWidth = null, cropHeight = null;
                    if (mediaFile.CropX.HasValue && mediaFile.CropY.HasValue && mediaFile.CropWidth.HasValue && mediaFile.CropHeight.HasValue
                        && inputWidth > 0 && inputHeight > 0)
                    {
                        var x = Math.Max(0, Math.Min(mediaFile.CropX.Value, inputWidth - 1));
                        var y = Math.Max(0, Math.Min(mediaFile.CropY.Value, inputHeight - 1));
                        var w = Math.Max(1, Math.Min(mediaFile.CropWidth.Value, inputWidth - x));
                        var h = Math.Max(1, Math.Min(mediaFile.CropHeight.Value, inputHeight - y));
                        cropX = x;
                        cropY = y;
                        cropWidth = w;
                        cropHeight = h;
                        isPortrait = h > w;
                        _logger.LogInformation("Applying crop: x={X}, y={Y}, w={W}, h={H}", x, y, w, h);
                    }

                    _logger.LogInformation(
                        "Video dimensions: {Width}x{Height}, Orientation: {Orientation}",
                        inputWidth, inputHeight, isPortrait ? "Portrait" : "Landscape");

                    // Use portrait variants for portrait videos (fills mobile screen), landscape for landscape
                    var qualityVariants = isPortrait
                        ? new List<VideoQualityVariant>
                        {
                            new VideoQualityVariant { Name = "1080p", Width = 1080, Height = 1920, Bitrate = 5000 },
                            new VideoQualityVariant { Name = "720p", Width = 720, Height = 1280, Bitrate = 2800 },
                            new VideoQualityVariant { Name = "480p", Width = 480, Height = 854, Bitrate = 1400 }
                        }
                        : new List<VideoQualityVariant>
                        {
                            new VideoQualityVariant { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
                            new VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2800 },
                            new VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1400 }
                        };

                    // Transcode to HLS
                    var hlsConfig = new HlsTranscodingConfigDto
                    {
                        InputFilePath = tempInputPath,
                        OutputDirectory = tempOutputDir,
                        OutputBaseName = "video",
                        SegmentDuration = 2,
                        QualityVariants = qualityVariants,
                        MuteVideo = mediaFile.IsMuted,
                        CropX = cropX,
                        CropY = cropY,
                        CropWidth = cropWidth,
                        CropHeight = cropHeight
                    };

                    var hlsResult = await _transcodingService.TranscodeToHlsAsync(hlsConfig);

                    if (!hlsResult.Success)
                    {
                        _logger.LogError("HLS transcoding failed for MediaFile {MediaFileId}: {Error}", 
                            mediaFileId, hlsResult.ErrorMessage);
                        return;
                    }

                    // Upload HLS files to S3
                    var bucketName = _configuration["Aws:S3UploadBucket"];
                    var folderPath = _configuration["Aws:PublicUploadFolder"];
                    var hlsGuid = Guid.NewGuid().ToString();
                    var hlsBasePath = $"{folderPath}/hls/{hlsGuid}";

                    await UploadHlsFilesToS3(tempOutputDir, hlsBasePath, bucketName);

                    // Update MediaFile with HLS information
                    var masterPlaylistKey = $"{hlsBasePath}/master.m3u8";
                    mediaFile.Url = masterPlaylistKey;
                    mediaFile.IsHlsProcessed = true;
                    mediaFile.HlsBasePath = hlsBasePath;
                    mediaFile.VideoDurationSeconds = (int)hlsResult.DurationSeconds;
                    mediaFile.AvailableQualities = string.Join(",", hlsResult.AvailableQualities);

                    await _dbContext.SaveChangesAsync(default);

                    _logger.LogInformation(
                        "HLS transcoding completed for MediaFile {MediaFileId}. Duration: {Duration}s, Qualities: {Qualities}",
                        mediaFileId, hlsResult.DurationSeconds, mediaFile.AvailableQualities);
                }
                finally
                {
                    // Cleanup temp files
                    try
                    {
                        if (System.IO.File.Exists(tempInputPath))
                            System.IO.File.Delete(tempInputPath);

                        if (System.IO.Directory.Exists(tempOutputDir))
                            System.IO.Directory.Delete(tempOutputDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup temp files for MediaFile {MediaFileId}", mediaFileId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing HLS transcoding for MediaFile {MediaFileId}", mediaFileId);
                throw;
            }
        }

        private async Task DownloadVideoFromS3(string s3KeyOrUrl, string localPath)
        {
            var bucketName = _configuration["Aws:S3UploadBucket"];
            
            // Extract S3 key from full URL if needed
            string s3Key = s3KeyOrUrl;
            if (s3KeyOrUrl.StartsWith("http"))
            {
                // URL format: https://bucket.s3.region.amazonaws.com/key
                var uri = new Uri(s3KeyOrUrl);
                s3Key = uri.AbsolutePath; // Keep the leading slash as-is
                
                // Extract bucket name from URL if different
                var host = uri.Host;
                if (host.Contains(".s3."))
                {
                    bucketName = host.Split('.')[0];
                }
            }
            else
            {
                // Database stores key without leading slash, but S3 has it WITH leading slash
                // So add it if missing (for files uploaded to bucket root)
                if (!s3Key.StartsWith("/"))
                {
                    s3Key = "/" + s3Key;
                }
            }
            
            _logger.LogInformation("Downloading video from S3: Bucket={Bucket}, Key={Key}", bucketName, s3Key);
            
            var s3Client = new Amazon.S3.AmazonS3Client(
                _configuration["Aws:AwsAccessKeyId"],
                _configuration["Aws:AwsSecretAccessKey"],
                AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion]));

            var request = new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = bucketName,
                Key = s3Key
            };

            try
            {
                using var response = await s3Client.GetObjectAsync(request);
                await response.WriteResponseStreamToFileAsync(localPath, false, default);
                _logger.LogInformation("Successfully downloaded video to {LocalPath}, Size: {Size} bytes", 
                    localPath, new System.IO.FileInfo(localPath).Length);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Failed to download from S3. Bucket={Bucket}, Key={Key}, StatusCode={StatusCode}", 
                    bucketName, s3Key, ex.StatusCode);
                throw;
            }
        }

        private async Task UploadHlsFilesToS3(string localDirectory, string s3BasePath, string bucketName)
        {
            using var client = new Amazon.S3.AmazonS3Client(
                _configuration["Aws:AwsAccessKeyId"],
                _configuration["Aws:AwsSecretAccessKey"],
                AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion]));

            var fileTransferUtility = new Amazon.S3.Transfer.TransferUtility(client);
            var files = System.IO.Directory.GetFiles(localDirectory, "*.*", System.IO.SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = System.IO.Path.GetRelativePath(localDirectory, file).Replace("\\", "/");
                var s3Key = $"{s3BasePath}/{relativePath}";

                var contentType = file.EndsWith(".m3u8") ? "application/vnd.apple.mpegurl" :
                                 file.EndsWith(".ts") ? "video/mp2t" :
                                 "application/octet-stream";

                var cacheControl = (file.EndsWith(".m3u8") || file.EndsWith(".ts"))
                    ? "public, max-age=3600"
                    : null;

                var uploadRequest = new Amazon.S3.Transfer.TransferUtilityUploadRequest
                {
                    FilePath = file,
                    Key = s3Key,
                    BucketName = bucketName,
                    ContentType = contentType
                };

                if (!string.IsNullOrEmpty(cacheControl))
                {
                    uploadRequest.Headers.CacheControl = cacheControl;
                }

                await fileTransferUtility.UploadAsync(uploadRequest);
                _logger.LogDebug("Uploaded HLS file: {S3Key}", s3Key);
            }

            _logger.LogInformation("Uploaded {FileCount} HLS files to S3 at {BasePath}", files.Length, s3BasePath);
        }
    }
}
