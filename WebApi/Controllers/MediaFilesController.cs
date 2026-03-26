using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models.MediaFiles;
using MediatR;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaFilesController : ApiControllerBase
    {
        //private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<MediaFilesController> _logger;

        //public MediaFilesController(IMediator mediator)
        //{
        //    _mediator = mediator;
        //}
        public MediaFilesController(IMapper mapper, ILogger<MediaFilesController> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<MediaFileDetailsResponse>>> UploadMediaFile()
        {
            // Log form data keys for debugging
            _logger.LogInformation($"Form data keys: {string.Join(", ", Request.Form.Keys)}");
            
            // Check if there are any files in the request
            if (!Request.Form.Files.Any())
            {
                _logger.LogWarning("No files found in the request");
                return BadRequest("No files were uploaded. Please include files in the 'Files' field.");
            }

            try
            {
                // Create a new DTO and populate it with the files from the request
                var muteVideo = false;
                var muteValue = Request.Form["MuteVideo"].FirstOrDefault();
                if (!string.IsNullOrEmpty(muteValue) && bool.TryParse(muteValue, out var mv))
                    muteVideo = mv;

                int? cropX = ParseIntForm("CropX"), cropY = ParseIntForm("CropY"),
                    cropWidth = ParseIntForm("CropWidth"), cropHeight = ParseIntForm("CropHeight");

                var dto = new UploadMediaFileDto
                {
                    Files = Request.Form.Files.ToList(),
                    MuteVideo = muteVideo,
                    CropX = cropX,
                    CropY = cropY,
                    CropWidth = cropWidth,
                    CropHeight = cropHeight,
                    FilterType = Request.Form["FilterType"].FirstOrDefault()
                };

                // Map the DTO to the command
                var command = _mapper.Map<UploadMediaFileCommand>(dto);
                
                // Send the command to the mediator
                var result = await Mediator.Send(command);
                
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                return BadRequest($"Error processing file upload: {ex.Message}");
            }
        }

        private int? ParseIntForm(string key)
        {
            var value = Request.Form[key].FirstOrDefault();
            return int.TryParse(value, out var v) ? v : (int?)null;
        }
    }
}
