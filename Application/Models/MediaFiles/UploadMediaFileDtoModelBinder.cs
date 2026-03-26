using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDtoModelBinder : IModelBinder
    {
        private readonly ILogger<UploadMediaFileDtoModelBinder> _logger;

        public UploadMediaFileDtoModelBinder(ILogger<UploadMediaFileDtoModelBinder> logger)
        {
            _logger = logger;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.LogInformation("Binding UploadMediaFileDto model");
            _logger.LogInformation($"Form keys: {string.Join(", ", bindingContext.HttpContext.Request.Form.Keys)}");
            _logger.LogInformation($"Files count: {bindingContext.HttpContext.Request.Form.Files.Count}");

            var model = new UploadMediaFileDto();
            var files = new List<IFormFile>();

            // Check if there are any files in the request
            if (bindingContext.HttpContext.Request.Form.Files.Count > 0)
            {
                foreach (var file in bindingContext.HttpContext.Request.Form.Files)
                {
                    files.Add(file);
                }
            }

            model.Files = files;

            // Bind MuteVideo from form
            var muteValue = bindingContext.HttpContext.Request.Form["MuteVideo"].FirstOrDefault();
            if (!string.IsNullOrEmpty(muteValue) && bool.TryParse(muteValue, out var muteVideo))
            {
                model.MuteVideo = muteVideo;
            }

            // Bind crop params from form
            if (int.TryParse(bindingContext.HttpContext.Request.Form["CropX"].FirstOrDefault(), out var cropX))
                model.CropX = cropX;
            if (int.TryParse(bindingContext.HttpContext.Request.Form["CropY"].FirstOrDefault(), out var cropY))
                model.CropY = cropY;
            if (int.TryParse(bindingContext.HttpContext.Request.Form["CropWidth"].FirstOrDefault(), out var cropWidth))
                model.CropWidth = cropWidth;
            if (int.TryParse(bindingContext.HttpContext.Request.Form["CropHeight"].FirstOrDefault(), out var cropHeight))
                model.CropHeight = cropHeight;

            // Bind FilterType from form
            model.FilterType = bindingContext.HttpContext.Request.Form["FilterType"].FirstOrDefault();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
} 