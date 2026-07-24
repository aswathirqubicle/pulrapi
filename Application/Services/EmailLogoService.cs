using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Application.Services
{
    /// <summary>
    /// Service for handling email logo conversion and attachment creation.
    /// Implements IEmailLogoService following Single Responsibility Principle.
    /// Follows Open/Closed Principle - can be extended without modification.
    /// </summary>
    public class EmailLogoService : IEmailLogoService
    {
        private readonly ILogger<EmailLogoService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private static byte[] _cachedLogoPngBytes = null;
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private const int MaxRetries = 3;
        private const int TimeoutSeconds = 30;

        public EmailLogoService(
            ILogger<EmailLogoService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc/>
        public async Task<EmailAttachment> CreateLogoAttachmentAsync()
        {
            var logoPngBytes = await GetLogoPngBytesAsync();
            
            if (logoPngBytes == null || logoPngBytes.Length == 0)
            {
                return null;
            }

            // Create a new stream for each attachment (each email needs its own stream)
            var logoStream = new MemoryStream(logoPngBytes);
            logoStream.Position = 0;

            return new EmailAttachment
            {
                Name = "pulr-logo.png",
                ContentStream = logoStream,
                ContentId = "pulr-logo-id@pulr.co",
                IsInline = true,
                MimeType = "image/png"
            };
        }

        /// <summary>
        /// Gets the PULR logo as PNG bytes directly from a URL.
        /// Thread-safe caching for performance.
        /// </summary>
        /// <returns>PNG bytes of the logo, or null if download fails</returns>
        private async Task<byte[]> GetLogoPngBytesAsync()
        {
            // Fast path: return cached bytes if available
            if (_cachedLogoPngBytes != null && _cachedLogoPngBytes.Length > 0)
            {
                return _cachedLogoPngBytes;
            }

            // Thread-safe caching: only one thread downloads at a time
            await _cacheLock.WaitAsync();
            try
            {
                // Double-check pattern
                if (_cachedLogoPngBytes != null && _cachedLogoPngBytes.Length > 0)
                {
                    return _cachedLogoPngBytes;
                }

                // Build logo URL from configuration (no hardcoded fallbacks)
                var logoBucket = _configuration[AwsLocationNames.S3DocumentsBucket];
                var logoFileName = _configuration[AwsLocationNames.LogoFileName];
                var logoRegion = _configuration[AwsLocationNames.AwsRegion];

                if (string.IsNullOrEmpty(logoBucket) || string.IsNullOrEmpty(logoFileName) || string.IsNullOrEmpty(logoRegion))
                {
                    _logger.LogError("Logo configuration missing. Required: S3DocumentsBucket, LogoFileName, AwsRegion");
                    return null;
                }

                var logoUrl = $"https://{logoBucket}.s3.{logoRegion}.amazonaws.com/{logoFileName}";

                _logger.LogInformation("Logo URL configured: {LogoUrl}", logoUrl);

                for (int attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        _logger.LogInformation("Fetching PNG logo (attempt {Attempt}/{MaxRetries}): {Url}", 
                            attempt, MaxRetries, logoUrl);

                        using var httpClient = _httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Pulr-EmailService/1.0");

                        var response = await httpClient.GetAsync(logoUrl);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var pngBytes = await response.Content.ReadAsByteArrayAsync();
                            _cachedLogoPngBytes = pngBytes;
                            _logger.LogInformation("Successfully fetched logo. Size: {Size} bytes", pngBytes.Length);
                            return pngBytes;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch logo. StatusCode: {StatusCode}", response.StatusCode);
                            if (attempt < MaxRetries) await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching logo on attempt {Attempt}", attempt);
                        if (attempt < MaxRetries) await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                    }
                }

                return null;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AddLogoAttachmentAsync(EmailParamsDto emailParams)
        {
            if (emailParams == null)
            {
                _logger.LogWarning("EmailParamsDto is null, cannot add logo attachment");
                return false;
            }

            // Check if logo attachment already exists
            var existingLogo = emailParams.Attachments?.FirstOrDefault(a => 
                a.ContentId == "pulr-logo-id@pulr.co" && a.IsInline);

            if (existingLogo != null)
            {
                _logger.LogDebug("Logo attachment already exists in email parameters");
                return false;
            }

            var logoAttachment = await CreateLogoAttachmentAsync();
            
            if (logoAttachment == null)
            {
                _logger.LogWarning("Failed to create logo attachment");
                return false;
            }

            if (emailParams.Attachments == null)
            {
                emailParams.Attachments = new System.Collections.Generic.List<EmailAttachment>();
            }

            emailParams.Attachments.Add(logoAttachment);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> PreloadLogoAsync()
        {
            _logger.LogInformation("Pre-loading logo into cache during application startup...");
            
            try
            {
                var logoBytes = await GetLogoPngBytesAsync();
                
                if (logoBytes != null && logoBytes.Length > 0)
                {
                    _logger.LogInformation("Logo successfully pre-loaded. Size: {Size} bytes", logoBytes.Length);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to pre-load logo. Logo will not be available in emails until it can be loaded.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while pre-loading logo: {Message}", ex.Message);
                return false;
            }
        }
    }
}
