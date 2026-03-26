using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IImageProcessingService _imageProcessingService;

        public FileUploadService(IConfiguration configuration,
            ILogger<FileUploadService> logger,
            IImageProcessingService imageProcessingService)
        {
            _configuration = configuration;
            _logger = logger;
            _imageProcessingService = imageProcessingService;
        }

        public async Task<string> UploadImage(FileUploadConfigDto config)
        {
            string tempInputPath = null;
            string tempOutputPath = null;
            try
            {
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(config.FileName)}");
                tempOutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(config.FileName)}");

                using (var fileStream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await config.File.CopyToAsync(fileStream);
                }

                var processedResult = await _imageProcessingService.ProcessImageAsync(
                    tempInputPath, tempOutputPath, config.ImageWidth, config.ImageHeight, config.FilterType);

                if (processedResult == null)
                {
                    // Fallback to uploading original if processing fails
                    using var memoryStream = new MemoryStream();
                    await config.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    string fallbackKey = await Upload(
                        memoryStream,
                        String.Format("{0}{1}",
                            Guid.NewGuid().ToString(),
                            Path.GetExtension(config.FileName)),
                         config
                    );
                    return fallbackKey.TrimStart('/');
                }

                using (var processedStream = new FileStream(processedResult, FileMode.Open, FileAccess.Read))
                {
                    string uploadedImageKey = await Upload(
                        processedStream,
                        String.Format("{0}{1}",
                            Guid.NewGuid().ToString(),
                            Path.GetExtension(config.FileName)),
                         config
                    );
                    return uploadedImageKey.TrimStart('/');
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> UploadVideo(FileUploadConfigDto config)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                config.File.CopyTo(memoryStream);
                
                // Upload original video first (as backup/fallback)
                string uploadedVideoKey = await Upload(
                    memoryStream,
                    String.Format("{0}{1}",
                        Guid.NewGuid().ToString(),
                        Path.GetExtension(config.FileName)),
                     config
                );

                uploadedVideoKey = uploadedVideoKey.TrimStart('/');
                return uploadedVideoKey;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Uploads video and transcodes to HLS format with multiple quality variants
        /// </summary>
        public async Task<(string originalUrl, string hlsMasterUrl, string hlsBasePath, int durationSeconds, int width, int height, string[] qualities)> UploadVideoWithHls(FileUploadConfigDto config, IVideoTranscodingService transcodingService)
        {
            string tempInputPath = null;
            string tempOutputDir = null;

            try
            {
                // Create temporary directory for processing
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(config.FileName)}");
                tempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempOutputDir);

                // Save uploaded file to temp location
                using (var fileStream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await config.File.CopyToAsync(fileStream);
                }

                // Upload original video to S3 (as backup)
                string originalVideoKey;
                using (var memoryStream = new MemoryStream())
                {
                    config.File.OpenReadStream().CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    
                    originalVideoKey = await Upload(
                        memoryStream,
                        String.Format("{0}_original{1}",
                            Guid.NewGuid().ToString(),
                            Path.GetExtension(config.FileName)),
                        config
                    );
                }

                // Detect input video dimensions for orientation-aware transcoding (Instagram-style)
                var (inputWidth, inputHeight) = await transcodingService.GetVideoDimensionsAsync(tempInputPath);
                var isPortrait = inputWidth > 0 && inputHeight > 0 && inputHeight > inputWidth;

                var qualityVariants = isPortrait
                    ? new List<Application.Models.VideoTranscoding.VideoQualityVariant>
                    {
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "1080p", Width = 1080, Height = 1920, Bitrate = 5000 },
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "720p", Width = 720, Height = 1280, Bitrate = 2800 },
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "480p", Width = 480, Height = 854, Bitrate = 1400 }
                    }
                    : new List<Application.Models.VideoTranscoding.VideoQualityVariant>
                    {
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2800 },
                        new Application.Models.VideoTranscoding.VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1400 }
                    };

                // Transcode to HLS (MuteVideo from config if available - FileUploadService doesn't have per-upload mute, defaults to false)
                var hlsConfig = new Application.Models.VideoTranscoding.HlsTranscodingConfigDto
                {
                    InputFilePath = tempInputPath,
                    OutputDirectory = tempOutputDir,
                    OutputBaseName = "video",
                    SegmentDuration = 2,
                    QualityVariants = qualityVariants,
                    MuteVideo = false
                };

                var hlsResult = await transcodingService.TranscodeToHlsAsync(hlsConfig);

                if (!hlsResult.Success)
                {
                    _logger.LogError("HLS transcoding failed: {Error}", hlsResult.ErrorMessage);
                    // Return original video URL as fallback
                    return (originalVideoKey.TrimStart('/'), null, null, 0, 0, 0, null);
                }

                // Upload HLS files to S3
                var hlsGuid = Guid.NewGuid().ToString();
                var hlsBasePath = $"{config.FolderPath}/hls/{hlsGuid}";
                
                await UploadHlsFilesToS3(tempOutputDir, hlsBasePath, config.BucketName);

                // Master playlist URL
                var masterPlaylistKey = $"{hlsBasePath}/master.m3u8";

                return (
                    originalVideoKey.TrimStart('/'),
                    masterPlaylistKey,
                    hlsBasePath,
                    (int)hlsResult.DurationSeconds,
                    0, // Width - will be provided by frontend
                    0, // Height - will be provided by frontend
                    hlsResult.AvailableQualities.ToArray()
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during HLS video upload");
                throw;
            }
            finally
            {
                // Cleanup temporary files
                try
                {
                    if (File.Exists(tempInputPath))
                        File.Delete(tempInputPath);
                    
                    if (Directory.Exists(tempOutputDir))
                        Directory.Delete(tempOutputDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temporary files");
                }
            }
        }

        private async Task UploadHlsFilesToS3(string localDirectory, string s3BasePath, string bucketName)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
            {
                var fileTransferUtility = new TransferUtility(client);

                // Upload all files in the directory (playlists and segments)
                var files = Directory.GetFiles(localDirectory, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(localDirectory, file).Replace("\\", "/");
                    var s3Key = $"{s3BasePath}/{relativePath}";

                    // Determine content type
                    var contentType = file.EndsWith(".m3u8") ? "application/vnd.apple.mpegurl" :
                                     file.EndsWith(".ts") ? "video/mp2t" :
                                     "application/octet-stream";

                    // Set cache control for HLS files (critical for iOS playback)
                    // iOS AVPlayer requires caching for smooth HLS streaming
                    var cacheControl = (file.EndsWith(".m3u8") || file.EndsWith(".ts")) 
                        ? "public, max-age=3600" // Cache for 1 hour
                        : null;

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        FilePath = file,
                        Key = s3Key,
                        BucketName = bucketName,
                        CannedACL = S3CannedACL.PublicRead,
                        ContentType = contentType
                    };

                    // Set cache control header for HLS files
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

        private async Task<string> Upload(Stream stream, string fileName, FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
            {
                // Build S3 key - keep the original behavior
                var s3Key = config.FolderPath + "/" + fileName;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = s3Key,
                    BucketName = config.BucketName,  
                    CannedACL = S3CannedACL.PublicRead
                };

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                return s3Key;
            }
        }

        public async Task<DeleteObjectResponse> Delete(FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
            {

                var deleteRequest = new DeleteObjectRequest
                {
                    // Key = config.OldFileName,
                    // BucketName = config.BucketName + "/" + config.FolderPath
                    Key = config.FolderPath + "/" + config.OldFileName,
                    BucketName = config.BucketName
                };

                var response = await client.DeleteObjectAsync(deleteRequest);

                return response;
            }
        }
        
        public async Task ListFilesInBucket(string bucketName, string prefixOrPath)
        {
            try
            {
                using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], RegionEndpoint.MESouth1))
                
                {
                    var listObjectsV2Paginator = client.Paginators.ListObjectsV2(new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        Prefix = prefixOrPath,
                        MaxKeys = 10 // how many items per page
                    });

                    var currentPageItemNames = new List<string>();
                    // we loop through all pages 
                    await foreach (var response in listObjectsV2Paginator.Responses)
                    {
                        var httpStatusCode = response.HttpStatusCode;
                        var numberOfKeys = response.KeyCount;
                        currentPageItemNames = response.S3Objects.Select(o => o.Key).ToList();
                    }


                }
            }
            catch (Exception e)
            {
                throw new Exception("Error listing files in bucket", e);
            }
        }
    }
}
