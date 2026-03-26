using System;
using System.IO;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Core.Infrastructure.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly ILogger<ImageProcessingService> _logger;

        public ImageProcessingService(ILogger<ImageProcessingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Always returns true — SkiaSharp is a NuGet dependency, no external binary needed.
        /// Kept for interface compatibility.
        /// </summary>
        public Task<bool> IsFfmpegAvailableAsync() => Task.FromResult(true);

        public Task<string> ProcessImageAsync(string inputPath, string outputPath, int width, int height, string filterType)
        {
            try
            {
                _logger.LogInformation("Starting SkiaSharp image processing for {InputFile}", inputPath);

                if (!File.Exists(inputPath))
                {
                    _logger.LogError("Input image file not found: {InputFilePath}", inputPath);
                    return Task.FromResult<string>(null);
                }

                using var inputStream = File.OpenRead(inputPath);
                using var originalBitmap = SKBitmap.Decode(inputStream);

                if (originalBitmap == null)
                {
                    _logger.LogError("Failed to decode image: {InputFilePath}", inputPath);
                    return Task.FromResult<string>(null);
                }

                // Scale to target size, covering the full area (Instagram-style center crop)
                var srcRatio = (float)originalBitmap.Width / originalBitmap.Height;
                var dstRatio = (float)width / height;

                int srcX, srcY, srcW, srcH;
                if (srcRatio > dstRatio)
                {
                    // Wider than target: crop sides
                    srcH = originalBitmap.Height;
                    srcW = (int)(srcH * dstRatio);
                    srcX = (originalBitmap.Width - srcW) / 2;
                    srcY = 0;
                }
                else
                {
                    // Taller than target: crop top/bottom
                    srcW = originalBitmap.Width;
                    srcH = (int)(srcW / dstRatio);
                    srcX = 0;
                    srcY = (originalBitmap.Height - srcH) / 2;
                }

                var srcRect = new SKRectI(srcX, srcY, srcX + srcW, srcY + srcH);
                var dstRect = new SKRect(0, 0, width, height);

                var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(imageInfo);
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Black);

                // Draw scaled + cropped image
                using var scaledPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
                canvas.DrawBitmap(originalBitmap, srcRect, dstRect, scaledPaint);

                // Apply filter overlay if specified
                var filterDef = MediaFilterHelper.GetSkiaFilter(filterType);
                if (filterDef != null)
                {
                    ApplyFilter(canvas, width, height, filterDef, filterType);
                }

                // Encode and save
                using var snapshot = surface.Snapshot();
                var format = GetOutputFormat(outputPath);
                using var encoded = snapshot.Encode(format, 90);

                if (encoded == null)
                {
                    _logger.LogError("Failed to encode output image: {OutputPath}", outputPath);
                    return Task.FromResult<string>(null);
                }

                using var outputStream = File.OpenWrite(outputPath);
                encoded.SaveTo(outputStream);

                _logger.LogInformation("SkiaSharp image processing completed for {InputFile}", inputPath);
                return Task.FromResult(outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SkiaSharp image processing for {InputFile}", inputPath);
                return Task.FromResult<string>(null);
            }
        }

        private static void ApplyFilter(SKCanvas canvas, int width, int height, SkiaFilterDefinition filterDef, string filterType)
        {
            var alpha = (byte)(filterDef.Opacity * 255);

            // For Mono filter: desaturate first by drawing a grayscale version on top
            if (filterType == "Mono")
            {
                using var grayPaint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0.33f, 0.33f, 0.33f, 0, 0,
                        0.33f, 0.33f, 0.33f, 0, 0,
                        0.33f, 0.33f, 0.33f, 0, 0,
                        0,     0,     0,     1, 0
                    }),
                    BlendMode = SKBlendMode.SrcOver
                };
                // Apply grayscale via color matrix on a solid overlay
                using var grayOverlay = new SKPaint
                {
                    Color = new SKColor(128, 128, 128, (byte)(filterDef.Opacity * 200)),
                    BlendMode = SKBlendMode.Saturation
                };
                canvas.DrawRect(0, 0, width, height, grayOverlay);
            }

            // Draw gradient overlay (matches frontend gradients[] exactly)
            var startColor = ParseHexColor(filterDef.GradientStart, alpha);
            var endColor = ParseHexColor(filterDef.GradientEnd, alpha);

            using var gradientPaint = new SKPaint { IsAntialias = false };
            gradientPaint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                new[] { startColor, endColor },
                SKShaderTileMode.Clamp);
            gradientPaint.BlendMode = SKBlendMode.Overlay;
            canvas.DrawRect(0, 0, width, height, gradientPaint);
        }

        private static SKColor ParseHexColor(string hex, byte alpha)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex[..2], 16);
                var g = Convert.ToByte(hex[2..4], 16);
                var b = Convert.ToByte(hex[4..6], 16);
                return new SKColor(r, g, b, alpha);
            }
            return SKColors.Transparent;
        }

        private static SKEncodedImageFormat GetOutputFormat(string outputPath)
        {
            var ext = Path.GetExtension(outputPath).ToLowerInvariant();
            return ext switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
        }
    }
}
