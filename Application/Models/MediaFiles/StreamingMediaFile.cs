using System.IO;

namespace Core.Application.Models.MediaFiles
{
    public class StreamingMediaFile
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Length { get; set; }
        public Stream Stream { get; set; }
    }
}
