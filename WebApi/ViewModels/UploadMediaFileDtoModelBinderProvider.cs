using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Core.Application.Models.MediaFiles;

namespace WebApi.ViewModels
{
    public class UploadMediaFileDtoModelBinderProvider : IModelBinderProvider
    {
        private readonly ILogger<UploadMediaFileDtoModelBinder> _logger;

        public UploadMediaFileDtoModelBinderProvider(ILogger<UploadMediaFileDtoModelBinder> logger)
        {
            _logger = logger;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(UploadMediaFileDto))
            {
                return new UploadMediaFileDtoModelBinder(_logger);
            }

            return null;
        }
    }
}
