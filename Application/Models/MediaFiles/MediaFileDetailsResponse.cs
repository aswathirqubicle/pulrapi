
namespace Core.Application.Models.MediaFiles
{
    public class MediaFileDetailsResponse
    {
        public string FileType { get; set; }
        public string Url { get; set; }
        public int Priority { get; set; }
        public string Uid { get; set; }

        // HLS Metadata fields (only populated for video files)
        public string OriginalUrl { get; set; }
        public bool? IsHlsProcessed { get; set; }
        public string HlsBasePath { get; set; }
        public int? VideoDurationSeconds { get; set; }
        public string AvailableQualities { get; set; }
        public bool? IsMuted { get; set; }
        public int? CropX { get; set; }
        public int? CropY { get; set; }
        public int? CropWidth { get; set; }
        public int? CropHeight { get; set; }
    }
}
