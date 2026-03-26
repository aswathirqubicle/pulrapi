using System;
using System.IO;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.VideoTranscoding;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Examples
{
    /// <summary>
    /// Example usage of the VideoTranscodingService for HLS conversion
    /// This is for reference only - the actual implementation is in FileUploadService
    /// </summary>
    public class HlsTranscodingExample
    {
        private readonly IVideoTranscodingService _transcodingService;
        private readonly ILogger<HlsTranscodingExample> _logger;

        public HlsTranscodingExample(
            IVideoTranscodingService transcodingService,
            ILogger<HlsTranscodingExample> logger)
        {
            _transcodingService = transcodingService;
            _logger = logger;
        }

        /// <summary>
        /// Example: Basic HLS transcoding with default settings
        /// </summary>
        public async Task<HlsTranscodingResultDto> BasicTranscodingExample(string inputVideoPath)
        {
            var config = new HlsTranscodingConfigDto
            {
                InputFilePath = inputVideoPath,
                OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                OutputBaseName = "video",
                SegmentDuration = 2 // 2-second segments
            };

            var result = await _transcodingService.TranscodeToHlsAsync(config);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Transcoding successful! Master playlist: {MasterPlaylist}, Duration: {Duration}s, Qualities: {Qualities}",
                    result.MasterPlaylistPath,
                    result.DurationSeconds,
                    string.Join(", ", result.AvailableQualities));
            }
            else
            {
                _logger.LogError("Transcoding failed: {Error}", result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Example: Custom quality variants
        /// </summary>
        public async Task<HlsTranscodingResultDto> CustomQualitiesExample(string inputVideoPath)
        {
            var config = new HlsTranscodingConfigDto
            {
                InputFilePath = inputVideoPath,
                OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                OutputBaseName = "video",
                SegmentDuration = 2,
                QualityVariants = new System.Collections.Generic.List<VideoQualityVariant>
                {
                    // 4K quality
                    new VideoQualityVariant { Name = "2160p", Width = 3840, Height = 2160, Bitrate = 15000 },
                    // 1440p quality
                    new VideoQualityVariant { Name = "1440p", Width = 2560, Height = 1440, Bitrate = 8000 },
                    // 1080p quality
                    new VideoQualityVariant { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
                    // 720p quality
                    new VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2800 },
                    // 480p quality
                    new VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1400 },
                    // 360p quality (for very slow connections)
                    new VideoQualityVariant { Name = "360p", Width = 640, Height = 360, Bitrate = 800 }
                }
            };

            return await _transcodingService.TranscodeToHlsAsync(config);
        }

        /// <summary>
        /// Example: Mobile-optimized quality variants (smaller file sizes)
        /// </summary>
        public async Task<HlsTranscodingResultDto> MobileOptimizedExample(string inputVideoPath)
        {
            var config = new HlsTranscodingConfigDto
            {
                InputFilePath = inputVideoPath,
                OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                OutputBaseName = "video",
                SegmentDuration = 2,
                QualityVariants = new System.Collections.Generic.List<VideoQualityVariant>
                {
                    new VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2000 },
                    new VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1000 },
                    new VideoQualityVariant { Name = "360p", Width = 640, Height = 360, Bitrate = 600 }
                },
                AudioBitrate = 96 // Lower audio bitrate for mobile
            };

            return await _transcodingService.TranscodeToHlsAsync(config);
        }

        /// <summary>
        /// Example: Get video duration before transcoding
        /// </summary>
        public async Task<double> GetVideoDurationExample(string videoPath)
        {
            var duration = await _transcodingService.GetVideoDurationAsync(videoPath);
            _logger.LogInformation("Video duration: {Duration} seconds", duration);
            return duration;
        }

        /// <summary>
        /// Example: Check FFmpeg availability
        /// </summary>
        public async Task<bool> CheckFfmpegExample()
        {
            var isAvailable = await _transcodingService.IsFfmpegAvailableAsync();
            
            if (isAvailable)
            {
                _logger.LogInformation("FFmpeg is available and ready for transcoding");
            }
            else
            {
                _logger.LogWarning("FFmpeg is not available. Please install FFmpeg.");
            }

            return isAvailable;
        }

        /// <summary>
        /// Example: Complete workflow with error handling
        /// </summary>
        public async Task<HlsTranscodingResultDto> CompleteWorkflowExample(string inputVideoPath, string outputDirectory)
        {
            try
            {
                // Step 1: Check FFmpeg availability
                if (!await _transcodingService.IsFfmpegAvailableAsync())
                {
                    throw new Exception("FFmpeg is not available");
                }

                // Step 2: Validate input file
                if (!File.Exists(inputVideoPath))
                {
                    throw new FileNotFoundException("Input video not found", inputVideoPath);
                }

                // Step 3: Get video duration
                var duration = await _transcodingService.GetVideoDurationAsync(inputVideoPath);
                _logger.LogInformation("Processing video with duration: {Duration}s", duration);

                // Step 4: Configure transcoding
                var config = new HlsTranscodingConfigDto
                {
                    InputFilePath = inputVideoPath,
                    OutputDirectory = outputDirectory,
                    OutputBaseName = "video",
                    SegmentDuration = 2,
                    EnableFastStart = true
                };

                // Step 5: Transcode
                var result = await _transcodingService.TranscodeToHlsAsync(config);

                // Step 6: Verify result
                if (result.Success)
                {
                    _logger.LogInformation(
                        "✅ Transcoding completed successfully!\n" +
                        "   Master Playlist: {MasterPlaylist}\n" +
                        "   Duration: {Duration}s\n" +
                        "   Qualities: {Qualities}\n" +
                        "   Total Segments: {Segments}",
                        result.MasterPlaylistPath,
                        result.DurationSeconds,
                        string.Join(", ", result.AvailableQualities),
                        result.TotalSegments);
                }
                else
                {
                    _logger.LogError("❌ Transcoding failed: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HLS transcoding workflow");
                throw;
            }
        }
    }
}
