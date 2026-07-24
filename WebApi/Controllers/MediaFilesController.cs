using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Core.Application.Mediatr.MediaFiles.Commands;
using Core.Application.Models.MediaFiles;
using MediatR;
using AutoMapper;
using WebApi.Filters;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MediaFilesController : ApiControllerBase
    {
        private const int MaxFiles = 10;
        private const long MaxFileSizeBytes = 30L * 1024 * 1024; // 30MB per file

        private readonly IMapper _mapper;
        private readonly ILogger<MediaFilesController> _logger;

        public MediaFilesController(IMapper mapper, ILogger<MediaFilesController> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(30 * 1024 * 1024)] // 30MB
        [DisableFormValueModelBinding]
        public async Task<ActionResult<List<MediaFileDetailsResponse>>> UploadMediaFile()
        {
            try
            {
                _logger.LogInformation("Starting streaming multipart upload");

                var files = new List<StreamingMediaFile>();
                bool muteVideo = false;
                int? cropX = null, cropY = null, cropWidth = null, cropHeight = null;
                string filterType = null;

                if (string.IsNullOrEmpty(Request.ContentType))
                {
                    _logger.LogWarning("Request Content-Type is null or empty");
                    return BadRequest("Content-Type header is required");
                }

                _logger.LogDebug("Parsing Content-Type: {ContentType}", Request.ContentType);
                var mediaTypeHeader = MediaTypeHeaderValue.Parse(Request.ContentType);
                // Boundary may come quoted; MultipartReader needs it unquoted
                var boundaryValue = mediaTypeHeader.Boundary.Value;
                if (boundaryValue.Length >= 2 && boundaryValue.StartsWith("\"") && boundaryValue.EndsWith("\""))
                {
                    boundaryValue = boundaryValue.Substring(1, boundaryValue.Length - 2);
                }

                if (string.IsNullOrWhiteSpace(boundaryValue))
                {
                    _logger.LogWarning("No boundary found in multipart content");
                    return BadRequest("Invalid multipart request");
                }

                var reader = new MultipartReader(boundaryValue, Request.Body);

                while (true)
                {
                    var section = await reader.ReadNextSectionAsync();
                    if (section == null) break;

                    if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                    {
                        if (contentDisposition.IsFileDisposition())
                        {
                            if (files.Count >= MaxFiles)
                            {
                                _logger.LogWarning("Upload rejected: more than {MaxFiles} files in request", MaxFiles);
                                return BadRequest($"A maximum of {MaxFiles} files can be uploaded per request.");
                            }

                            var fileName = contentDisposition.FileName.Value?.Trim('"') ?? "unnamed";
                            var contentType = section.ContentType;

                            // Copy section body into a MemoryStream BEFORE advancing to next section,
                            // aborting mid-copy if the per-file size cap is exceeded so we never
                            // buffer an oversized payload in memory.
                            var stream = new MemoryStream();
                            var buffer = new byte[81920];
                            long copied = 0;
                            int bytesRead;
                            while ((bytesRead = await section.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                copied += bytesRead;
                                if (copied > MaxFileSizeBytes)
                                {
                                    stream.Dispose();
                                    _logger.LogWarning("Upload rejected: file '{FileName}' exceeds {MaxBytes} bytes", fileName, MaxFileSizeBytes);
                                    return BadRequest($"File '{fileName}' exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
                                }
                                await stream.WriteAsync(buffer, 0, bytesRead);
                            }
                            stream.Position = 0;

                            files.Add(new StreamingMediaFile
                            {
                                FileName = fileName,
                                ContentType = contentType,
                                Length = stream.Length,
                                Stream = stream
                            });
                        }
                        else if (contentDisposition.IsFormDisposition())
                        {
                            var name = contentDisposition.Name.Value?.Trim('"');

                            // Read section body bytes directly — do NOT use StreamReader which buffers past the section body
                            var formStream = new MemoryStream();
                            await section.Body.CopyToAsync(formStream);
                            var formData = System.Text.Encoding.UTF8.GetString(formStream.ToArray()).Trim();

                            switch (name)
                            {
                                case "MuteVideo" when bool.TryParse(formData, out var mv):
                                    muteVideo = mv;
                                    break;
                                case "CropX" when int.TryParse(formData, out var cx):
                                    cropX = cx;
                                    break;
                                case "CropY" when int.TryParse(formData, out var cy):
                                    cropY = cy;
                                    break;
                                case "CropWidth" when int.TryParse(formData, out var cw):
                                    cropWidth = cw;
                                    break;
                                case "CropHeight" when int.TryParse(formData, out var ch):
                                    cropHeight = ch;
                                    break;
                                case "FilterType":
                                    filterType = formData;
                                    break;
                            }
                        }
                    }
                }

                if (files.Count == 0)
                {
                    _logger.LogWarning("No files found in the request");
                    return BadRequest("No files were uploaded. Please include files in the 'Files' field.");
                }

                _logger.LogInformation("Received {FileCount} files via streaming", files.Count);

                var command = new UploadMediaFileCommand
                {
                    Files = files,
                    MuteVideo = muteVideo,
                    CropX = cropX,
                    CropY = cropY,
                    CropWidth = cropWidth,
                    CropHeight = cropHeight,
                    FilterType = filterType
                };

                var result = await Mediator.Send(command);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return BadRequest(new { message = "Error processing file upload. Please verify the file and try again.", traceId = HttpContext.TraceIdentifier });
            }
        }
    }
}
