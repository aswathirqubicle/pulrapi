using System.Threading.Tasks;
using Core.Application.Models.VideoTranscoding;

namespace Core.Application.Interfaces
{
    public interface IVideoTranscodingService
    {
        /// <summary>
        /// Transcodes a video to HLS format with multiple quality variants
        /// </summary>
        /// <param name="config">Transcoding configuration</param>
        /// <returns>Result containing paths and metadata</returns>
        Task<HlsTranscodingResultDto> TranscodeToHlsAsync(HlsTranscodingConfigDto config);

        /// <summary>
        /// Gets the dimensions (width, height) of a video file
        /// </summary>
        Task<(int Width, int Height)> GetVideoDimensionsAsync(string filePath);

        /// <summary>
        /// Checks if FFmpeg is installed and available
        /// </summary>
        /// <returns>True if FFmpeg is available</returns>
        Task<bool> IsFfmpegAvailableAsync();

        /// <summary>
        /// Gets the duration of a video file in seconds
        /// </summary>
        /// <param name="filePath">Path to video file</param>
        /// <returns>Duration in seconds</returns>
        Task<double> GetVideoDurationAsync(string filePath);
    }
}
