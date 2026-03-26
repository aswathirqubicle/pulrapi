using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Application.Models.MediaFiles
{
    public class UploadMediaFileDtoModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(UploadMediaFileDto))
            {
                return new UploadMediaFileDtoModelBinder(context.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UploadMediaFileDtoModelBinder>>());
            }

            return null;
        }
    }
} 
