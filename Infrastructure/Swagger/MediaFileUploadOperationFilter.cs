using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Infrastructure.Swagger
{
    public class MediaFileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasUploadEndpoint = context.MethodInfo.DeclaringType?.Name == "MediaFilesController"
                && context.MethodInfo.Name == "UploadMediaFile";

            if (!hasUploadEndpoint)
                return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Required = new HashSet<string> { "Files" },
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["Files"] = new OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    },
                                    Description = "Media files to upload"
                                },
                                ["MuteVideo"] = new OpenApiSchema
                                {
                                    Type = "boolean",
                                    Default = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                                    Description = "When true, transcoded HLS video will have no audio (muted)"
                                },
                                ["CropX"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Nullable = true,
                                    Description = "Crop region X (left) in pixels"
                                },
                                ["CropY"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Nullable = true,
                                    Description = "Crop region Y (top) in pixels"
                                },
                                ["CropWidth"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Nullable = true,
                                    Description = "Crop region width in pixels"
                                },
                                ["CropHeight"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Nullable = true,
                                    Description = "Crop region height in pixels"
                                },
                                ["FilterType"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Nullable = true,
                                    Description = "Filter type to apply (e.g., Sunfade, Mono, Retro)"
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}