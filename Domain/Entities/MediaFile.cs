using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class MediaFile : EntityBase
    {
        [Required]
        public string Url { get; set; }
        [Required]
        public MediaFileTypeEnum MediaFileType { get; set; }
        [Required]
        public int Priority { get; set; } = 0;

        // HLS-specific fields for adaptive video streaming
        /// <summary>
        /// Original uploaded video URL (backup for progressive download fallback)
        /// </summary>
        public string OriginalUrl { get; set; }

        /// <summary>
        /// Indicates if video has been transcoded to HLS format
        /// </summary>
        public bool IsHlsProcessed { get; set; } = false;

        /// <summary>
        /// Base path in S3 where HLS segments and playlists are stored
        /// </summary>
        public string HlsBasePath { get; set; }

        /// <summary>
        /// Duration of the video in seconds (for videos only)
        /// </summary>
        public int? VideoDurationSeconds { get; set; }

        /// <summary>
        /// Comma-separated list of available quality variants (e.g., "1080p,720p,480p")
        /// </summary>
        public string AvailableQualities { get; set; }

        /// <summary>
        /// When true, HLS transcoding will produce video without audio (muted)
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Crop region X (left) in pixels. When all crop fields are set, applied before HLS transcoding.
        /// </summary>
        public int? CropX { get; set; }

        /// <summary>
        /// Crop region Y (top) in pixels.
        /// </summary>
        public int? CropY { get; set; }

        /// <summary>
        /// Crop region width in pixels.
        /// </summary>
        public int? CropWidth { get; set; }

        /// <summary>
        /// Crop region height in pixels.
        /// </summary>
        public int? CropHeight { get; set; }

        /// <summary>
        /// Filter to apply (e.g. Sunfade, Mono, Retro)
        /// </summary>
        public string FilterType { get; set; }
    }
}
