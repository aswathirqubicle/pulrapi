using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Core.Domain.Enums;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDto
    {
        [Required(ErrorMessage = "Files are required")]
        [MaxFileSize(30 * 1024 * 1024, "Video")] // 30MB for videos
        [FromForm(Name = "Files")]
        public List<IFormFile> Files { get; set; }

        /// <summary>
        /// When true, transcoded HLS video will have no audio (muted)
        /// </summary>
        [FromForm(Name = "MuteVideo")]
        public bool MuteVideo { get; set; }

        /// <summary>
        /// Crop region X (left) in pixels. All four crop fields must be set for crop to apply.
        /// </summary>
        [FromForm(Name = "CropX")]
        public int? CropX { get; set; }

        /// <summary>
        /// Crop region Y (top) in pixels.
        /// </summary>
        [FromForm(Name = "CropY")]
        public int? CropY { get; set; }

        /// <summary>
        /// Crop region width in pixels.
        /// </summary>
        [FromForm(Name = "CropWidth")]
        public int? CropWidth { get; set; }

        /// <summary>
        /// Crop region height in pixels.
        /// </summary>
        [FromForm(Name = "CropHeight")]
        public int? CropHeight { get; set; }

        /// <summary>
        /// Filter type to apply to the media (e.g., Sunfade, Mono, Retro)
        /// </summary>
        [FromForm(Name = "FilterType")]
        public string FilterType { get; set; }
    }
}
