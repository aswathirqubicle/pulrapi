using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;

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
            if (config.FileStream != null)
                return await UploadImageAsync(config);
            else if (config.File != null)
                return await UploadImageFileAsync(config);
            else
                throw new ArgumentException("No file provided");
        }

        private async Task<string> UploadImageAsync(FileUploadConfigDto config)
        {
            try
            {
                var processedStream = await _imageProcessingService.ProcessImageFromStreamAsync(
                    config.FileStream, config.ImageWidth, config.ImageHeight, config.FilterType);

                if (processedStream == null)
                {
                    config.FileStream.Position = 0;
                    return await Upload(config.FileStream, config.FileName, config);
                }

                return await Upload(processedStream, config.FileName, config);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> UploadImageFileAsync(FileUploadConfigDto config)
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
                    using var memoryStream = new MemoryStream();
                    await config.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    string fallbackKey = await Upload(memoryStream, config.FileName, config);
                    return fallbackKey.TrimStart('/');
                }

                using (var processedStream = new FileStream(processedResult, FileMode.Open, FileAccess.Read))
                {
                    string uploadedImageKey = await Upload(processedStream, config.FileName, config);
                    return uploadedImageKey.TrimStart('/');
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
            finally
            {
                try
                {
                    if (tempInputPath != null && File.Exists(tempInputPath))
                        File.Delete(tempInputPath);
                    if (tempOutputPath != null && File.Exists(tempOutputPath))
                        File.Delete(tempOutputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp files");
                }
            }
        }

        public async Task<string> UploadDocument(FileUploadConfigDto config)
        {
            return await UploadVideo(config);
        }

        public async Task<string> UploadVideo(FileUploadConfigDto config)
        {
            if (config.FileStream != null)
                return await UploadVideoAsync(config);
            else if (config.File != null)
                return await UploadVideoFileAsync(config);
            else
                throw new ArgumentException("No file provided");
        }

        private async Task<string> UploadVideoAsync(FileUploadConfigDto config)
        {
            try
            {
                config.FileStream.Position = 0;
                string uploadedVideoKey = await Upload(
                    config.FileStream,
                    config.FileName,
                    config
                );

                return uploadedVideoKey.TrimStart('/');
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> UploadVideoFileAsync(FileUploadConfigDto config)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                config.File.CopyTo(memoryStream);
                
                string uploadedVideoKey = await Upload(
                    memoryStream,
                    config.FileName,
                    config
                );

                return uploadedVideoKey.TrimStart('/');
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<(string originalUrl, string hlsMasterUrl, string hlsBasePath, int durationSeconds, int width, int height, string[] qualities)> UploadVideoWithHls(FileUploadConfigDto config, IVideoTranscodingService transcodingService)
        {
            string tempInputPath = null;
            string tempOutputDir = null;

            try
            {
                tempInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(config.FileName)}");
                tempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempOutputDir);

                // Save uploaded file to temp location
                if (config.FileStream != null)
                {
                    using (var fileStream = new FileStream(tempInputPath, FileMode.Create))
                    {
                        config.FileStream.Position = 0;
                        await config.FileStream.CopyToAsync(fileStream);
                    }
                }
                else if (config.File != null)
                {
                    using (var fileStream = new FileStream(tempInputPath, FileMode.Create))
                    {
                        await config.File.CopyToAsync(fileStream);
                    }
                }

                // Upload original video to S3 (as backup)
                string originalVideoKey;
                if (config.FileStream != null)
                {
                    config.FileStream.Position = 0;
                    originalVideoKey = await Upload(config.FileStream, config.FileName, config);
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    config.File.OpenReadStream().CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    originalVideoKey = await Upload(memoryStream, config.FileName, config);
                }

                // Transcode to HLS
                var (inputWidth, inputHeight) = await transcodingService.GetVideoDimensionsAsync(tempInputPath);
                var isPortrait = inputWidth > 0 && inputHeight > 0 && inputHeight > inputWidth;

                var qualityVariants = isPortrait
                    ? new List<Core.Application.Models.VideoTranscoding.VideoQualityVariant>
                    {
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "1080p", Width = 1080, Height = 1920, Bitrate = 5000 },
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "720p", Width = 720, Height = 1280, Bitrate = 2800 },
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "480p", Width = 480, Height = 854, Bitrate = 1400 }
                    }
                    : new List<Core.Application.Models.VideoTranscoding.VideoQualityVariant>
                    {
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2800 },
                        new Core.Application.Models.VideoTranscoding.VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1400 }
                    };

                var hlsConfig = new Core.Application.Models.VideoTranscoding.HlsTranscodingConfigDto
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
                    return (originalVideoKey.TrimStart('/'), null, null, 0, 0, 0, null);
                }

                // Upload HLS files to S3
                var hlsGuid = Guid.NewGuid().ToString();
                var hlsBasePath = $"{config.FolderPath}/hls/{hlsGuid}";
                
                await UploadHlsFilesToS3(tempOutputDir, hlsBasePath, config.BucketName);

                var masterPlaylistKey = $"{hlsBasePath}/master.m3u8";

                return (
                    originalVideoKey.TrimStart('/'),
                    masterPlaylistKey,
                    hlsBasePath,
                    (int)hlsResult.DurationSeconds,
                    0,
                    0,
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
                       _configuration["Aws:AwsSecretAccessKey"], 
                       AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion])))
            {
                var fileTransferUtility = new TransferUtility(client);

                var files = Directory.GetFiles(localDirectory, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(localDirectory, file).Replace("\\", "/");
                    var s3Key = $"{s3BasePath}/{relativePath}";

                    var contentType = file.EndsWith(".m3u8") ? "application/vnd.apple.mpegurl" :
                                     file.EndsWith(".ts") ? "video/mp2t" :
                                     "application/octet-stream";

                    var cacheControl = (file.EndsWith(".m3u8") || file.EndsWith(".ts")) 
                        ? "public, max-age=3600"
                        : null;

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        FilePath = file,
                        Key = s3Key,
                        BucketName = bucketName,
                        ContentType = contentType,
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
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

        private async Task<string> Upload(Stream stream, string fileName, FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], 
                       AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion])))
            {
                var safeExtension = Path.GetExtension(fileName).ToLowerInvariant();
                var s3Key = config.FolderPath + "/" + Guid.NewGuid().ToString("N") + safeExtension;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = s3Key,
                    BucketName = config.BucketName,
                    ContentType = GetContentType(safeExtension),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                return s3Key;
            }
        }

        private static string GetContentType(string extension)
        {
            // extension includes the leading dot (e.g. ".mp4") and is already lower-cased
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".webp":
                    return "image/webp";
                case ".avif":
                    return "image/avif";
                case ".mp4":
                    return "video/mp4";
                case ".webm":
                    return "video/webm";
                case ".ogg":
                    return "video/ogg";
                case ".avi":
                    return "video/x-msvideo";
                case ".wmv":
                    return "video/x-ms-wmv";
                case ".mpg":
                case ".mpeg":
                    return "video/mpeg";
                case ".pdf":
                    return "application/pdf";
                default:
                    return "application/octet-stream";
            }
        }

        public async Task<DeleteObjectResponse> Delete(FileUploadConfigDto config)
        {
            using (var client = new AmazonS3Client(_configuration["Aws:AwsAccessKeyId"],
                       _configuration["Aws:AwsSecretAccessKey"], 
                       AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion])))
            {

                var deleteRequest = new DeleteObjectRequest
                {
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
                       _configuration["Aws:AwsSecretAccessKey"], 
                       AwsRegionHelper.GetRegionEndpoint(_configuration[AwsLocationNames.AwsRegion])))
                
            {
                    var listObjectsV2Paginator = client.Paginators.ListObjectsV2(new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        Prefix = prefixOrPath,
                        MaxKeys = 10
                    });

                    var currentPageItemNames = new List<string>();
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
