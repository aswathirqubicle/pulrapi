using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Interfaces;

namespace Core.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoTestController : ControllerBase
    {
        private readonly IVideoTranscodingService _videoTranscodingService;

        public VideoTestController(IVideoTranscodingService videoTranscodingService)
        {
            _videoTranscodingService = videoTranscodingService;
        }

        /// <summary>
        /// Test if FFmpeg is installed and available
        /// </summary>
        [HttpGet("ffmpeg-status")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckFfmpegStatus()
        {
            var isAvailable = await _videoTranscodingService.IsFfmpegAvailableAsync();
            
            return Ok(new
            {
                ffmpegAvailable = isAvailable,
                message = isAvailable 
                    ? "FFmpeg is installed and ready for HLS transcoding" 
                    : "FFmpeg is not available. Please install FFmpeg to enable HLS video streaming."
            });
        }
    }
}
