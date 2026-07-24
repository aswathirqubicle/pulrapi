using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDto
    {
        [Required(ErrorMessage = "Files are required")]
        public List<StreamingMediaFile> Files { get; set; }

        public bool MuteVideo { get; set; }
        public int? CropX { get; set; }
        public int? CropY { get; set; }
        public int? CropWidth { get; set; }
        public int? CropHeight { get; set; }
        public string FilterType { get; set; }
    }
}
