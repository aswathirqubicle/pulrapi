using System.Collections.Generic;

namespace Core.Application.Models.VideoTranscoding
{
    public class HlsTranscodingConfigDto
    {
        /// <summary>
        /// Path to the input video file
        /// </summary>
        public string InputFilePath { get; set; }

        /// <summary>
        /// Directory where HLS output will be stored
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Base name for output files (without extension)
        /// </summary>
        public string OutputBaseName { get; set; }

        /// <summary>
        /// Segment duration in seconds (default: 2)
        /// </summary>
        public int SegmentDuration { get; set; } = 2;

        /// <summary>
        /// Quality variants to generate
        /// </summary>
        public List<VideoQualityVariant> QualityVariants { get; set; } = new List<VideoQualityVariant>
        {
            new VideoQualityVariant { Name = "1080p", Width = 1920, Height = 1080, Bitrate = 5000 },
            new VideoQualityVariant { Name = "720p", Width = 1280, Height = 720, Bitrate = 2800 },
            new VideoQualityVariant { Name = "480p", Width = 854, Height = 480, Bitrate = 1400 }
        };

        /// <summary>
        /// Audio bitrate in kbps (default: 128)
        /// </summary>
        public int AudioBitrate { get; set; } = 128;

        /// <summary>
        /// Whether to enable fast start (move metadata to front)
        /// </summary>
        public bool EnableFastStart { get; set; } = true;

        /// <summary>
        /// When true, output video will have no audio (muted)
        /// </summary>
        public bool MuteVideo { get; set; }

        /// <summary>
        /// Crop region X (left) in pixels. Applied before scale when all crop fields are set.
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
        /// Filter type to apply to the video (e.g., Sunfade, Mono, Retro)
        /// </summary>
        public string FilterType { get; set; }
    }

    public class VideoQualityVariant
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Bitrate { get; set; } // in kbps
    }
}
