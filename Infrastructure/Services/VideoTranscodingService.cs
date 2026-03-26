using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.VideoTranscoding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class VideoTranscodingService : IVideoTranscodingService
    {
        private readonly ILogger<VideoTranscodingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _ffmpegPath;

        public VideoTranscodingService(
            ILogger<VideoTranscodingService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Get FFmpeg path from configuration or use default
            _ffmpegPath = _configuration["FFmpeg:Path"] ?? "ffmpeg";
        }

        public async Task<bool> IsFfmpegAvailableAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg is not available");
                return false;
            }
        }

        public async Task<double> GetVideoDurationAsync(string filePath)
        {
            try
            {
                var arguments = $"-i \"{filePath}\" -hide_banner";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Parse duration from FFmpeg output
                var match = Regex.Match(stderr, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (match.Success)
                {
                    var hours = int.Parse(match.Groups[1].Value);
                    var minutes = int.Parse(match.Groups[2].Value);
                    var seconds = double.Parse(match.Groups[3].Value);
                    
                    return hours * 3600 + minutes * 60 + seconds;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get video duration for {FilePath}", filePath);
                return 0;
            }
        }

        public async Task<(int Width, int Height)> GetVideoDimensionsAsync(string filePath)
        {
            try
            {
                var arguments = $"-i \"{filePath}\" -hide_banner";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var match = Regex.Match(stderr, @"Stream.*Video:.*?(\d{2,5})x(\d{2,5})");
                if (match.Success)
                {
                    var width = int.Parse(match.Groups[1].Value);
                    var height = int.Parse(match.Groups[2].Value);
                    return (width, height);
                }

                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get video dimensions for {FilePath}", filePath);
                return (0, 0);
            }
        }

        public async Task<HlsTranscodingResultDto> TranscodeToHlsAsync(HlsTranscodingConfigDto config)
        {
            var result = new HlsTranscodingResultDto();

            try
            {
                _logger.LogInformation("Starting HLS transcoding for {InputFile}", config.InputFilePath);

                // Validate input file exists
                if (!File.Exists(config.InputFilePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Input file not found: {config.InputFilePath}";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // Check FFmpeg availability
                if (!await IsFfmpegAvailableAsync())
                {
                    result.Success = false;
                    result.ErrorMessage = "FFmpeg is not available. Please install FFmpeg.";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // Get video duration
                result.DurationSeconds = await GetVideoDurationAsync(config.InputFilePath);

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(config.OutputDirectory);

                // Generate HLS with multiple quality variants
                var transcodeSuccess = await TranscodeMultipleQualitiesAsync(config, result);

                if (!transcodeSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = "Transcoding failed";
                    return result;
                }

                result.Success = true;
                result.MasterPlaylistPath = Path.Combine(config.OutputDirectory, "master.m3u8");
                result.HlsBasePath = config.OutputDirectory;
                result.AvailableQualities = config.QualityVariants.Select(q => q.Name).ToList();

                _logger.LogInformation("HLS transcoding completed successfully for {InputFile}", config.InputFilePath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during HLS transcoding for {InputFile}", config.InputFilePath);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<bool> TranscodeMultipleQualitiesAsync(HlsTranscodingConfigDto config, HlsTranscodingResultDto result)
        {
            try
            {
                // Build FFmpeg command for adaptive HLS with multiple quality variants
                var arguments = BuildFfmpegCommand(config);

                _logger.LogInformation("Executing FFmpeg command: {Command}", $"{_ffmpegPath} {arguments}");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogDebug("FFmpeg output: {Output}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogDebug("FFmpeg error: {Error}", e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg process exited with code {ExitCode}", process.ExitCode);
                    return false;
                }

                // Count total segments generated
                result.TotalSegments = CountGeneratedSegments(config.OutputDirectory);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during FFmpeg transcoding");
                return false;
            }
        }

        private string BuildFfmpegCommand(HlsTranscodingConfigDto config)
        {
            var arguments = new List<string>();

            // Input file
            arguments.Add($"-i \"{config.InputFilePath}\"");

            // When muted: add silent audio source (anullsrc) - use index 1 for audio mapping
            if (config.MuteVideo)
            {
                arguments.Add("-f lavfi -i anullsrc=r=44100:cl=stereo");
            }

            // Fast start flag (move metadata to front)
            if (config.EnableFastStart)
            {
                arguments.Add("-movflags +faststart");
            }

            // Video codec settings
            arguments.Add("-c:v libx264");
            arguments.Add("-preset fast");
            arguments.Add("-profile:v main");
            arguments.Add("-level 4.0");

            // Audio codec settings
            arguments.Add("-c:a aac");
            arguments.Add($"-b:a {config.AudioBitrate}k");
            arguments.Add("-ac 2"); // Stereo

            // Map streams for each quality variant (use 1:a when muted = anullsrc)
            var audioMap = config.MuteVideo ? "1:a" : "0:a?";
            for (int i = 0; i < config.QualityVariants.Count; i++)
            {
                arguments.Add("-map 0:v");
                arguments.Add($"-map {audioMap}");
            }

            // Set resolution and bitrate for each variant
            // When user crop provided: crop user region only, then scale to variant (no center crop - preserves zoom/pan).
            // When no crop: Instagram-style scale to cover + center crop.
            var hasCrop = config.CropX.HasValue && config.CropY.HasValue && config.CropWidth.HasValue && config.CropHeight.HasValue;

            for (int i = 0; i < config.QualityVariants.Count; i++)
            {
                var variant = config.QualityVariants[i];
                var scaleFilter = hasCrop
                    ? $"crop={config.CropWidth}:{config.CropHeight}:{config.CropX}:{config.CropY},scale={variant.Width}:{variant.Height},setsar=1"
                    : $"scale={variant.Width}:{variant.Height}:force_original_aspect_ratio=increase,crop={variant.Width}:{variant.Height}:(iw-ow)/2:(ih-oh)/2,setsar=1";
                arguments.Add($"-filter:v:{i} \"{scaleFilter}\"");
                arguments.Add($"-b:v:{i} {variant.Bitrate}k");
                arguments.Add($"-maxrate:v:{i} {variant.Bitrate * 1.2}k");
                arguments.Add($"-bufsize:v:{i} {variant.Bitrate * 2}k");
            }

            // HLS settings
            arguments.Add("-f hls");
            arguments.Add($"-hls_time {config.SegmentDuration}");
            arguments.Add("-hls_playlist_type vod");
            arguments.Add("-hls_flags independent_segments");
            arguments.Add("-hls_segment_type mpegts");
            arguments.Add("-shortest");

            // Use %v placeholder for variant-specific paths
            var segmentFilename = Path.Combine(config.OutputDirectory, "%v", "segment_%03d.ts");
            arguments.Add($"-hls_segment_filename \"{segmentFilename}\"");

            // Use var_stream_map for multiple variants with quality names
            var streamMapParts = new List<string>();
            for (int i = 0; i < config.QualityVariants.Count; i++)
            {
                var variant = config.QualityVariants[i];
                // Create quality directory
                var qualityDir = Path.Combine(config.OutputDirectory, variant.Name);
                Directory.CreateDirectory(qualityDir);
                
                streamMapParts.Add($"v:{i},a:{i},name:{variant.Name}");
            }
            var streamMap = string.Join(" ", streamMapParts);
            arguments.Add($"-var_stream_map \"{streamMap}\"");

            // Master playlist
            arguments.Add($"-master_pl_name master.m3u8");

            // Output pattern with %v placeholder
            var outputPattern = Path.Combine(config.OutputDirectory, "%v", "playlist.m3u8");
            arguments.Add($"\"{outputPattern}\"");

            return string.Join(" ", arguments);
        }

        private int CountGeneratedSegments(string outputDirectory)
        {
            try
            {
                var segmentFiles = Directory.GetFiles(outputDirectory, "*.ts", SearchOption.AllDirectories);
                return segmentFiles.Length;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to count segments in {Directory}", outputDirectory);
                return 0;
            }
        }
    }
}
