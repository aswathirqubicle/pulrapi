using System.Collections.Generic;

namespace Pulr.Contracts.DTOs.VideoTranscoding
{
    public class HlsTranscodingResultDto
    {
        /// <summary>
        /// Path to the master playlist (.m3u8)
        /// </summary>
        public string MasterPlaylistPath { get; set; }

        /// <summary>
        /// S3 URL to the master playlist
        /// </summary>
        public string MasterPlaylistUrl { get; set; }

        /// <summary>
        /// Base path in S3 where all HLS files are stored
        /// </summary>
        public string HlsBasePath { get; set; }

        /// <summary>
        /// Duration of the video in seconds
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// List of quality variants that were generated
        /// </summary>
        public List<string> AvailableQualities { get; set; } = new List<string>();

        /// <summary>
        /// Total number of segments generated across all qualities
        /// </summary>
        public int TotalSegments { get; set; }

        /// <summary>
        /// Whether transcoding was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if transcoding failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
