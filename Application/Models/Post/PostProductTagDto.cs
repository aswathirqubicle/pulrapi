using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Post
{
    public class PostProductTagDto
    {
        [Required]
        public string ProductUid { get; set; }
        
        // Pixel coordinates from frontend
        [Range(0, 2000, ErrorMessage = "LocationX must be between 0 and 2000 pixels")]
        public double LocationX { get; set; }
        
        [Range(0, 2000, ErrorMessage = "LocationY must be between 0 and 2000 pixels")]
        public double LocationY { get; set; }
        
        // Image dimensions for coordinate conversion
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }
        public string ThumbnailUid { get; set; }
    }
}
