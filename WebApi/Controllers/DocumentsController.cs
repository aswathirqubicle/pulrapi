using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Core.Application.Constants;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DocumentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public DocumentsController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("terms-of-service")]
        public Task<IActionResult> GetTermsOfService()
            => ProxyDocument("Term of Service.pdf", "Terms of Service.pdf");

        [HttpGet("eula-agreement")]
        public Task<IActionResult> GetEulaAgreement()
            => ProxyDocument("EULA Agreement.pdf", "EULA Agreement.pdf");

        [HttpGet("open-source-license")]
        public Task<IActionResult> GetOpenSourceLicense()
            => ProxyDocument("Pulr App Open Source License.pdf", "Open Source License.pdf");

        private async Task<IActionResult> ProxyDocument(string key, string downloadName)
        {
            var url = BuildDocumentUrl(key);
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, $"Document unavailable (S3 returned {(int)response.StatusCode})");

            var stream = await response.Content.ReadAsStreamAsync();
            return File(stream, "application/pdf", downloadName);
        }

        private string BuildDocumentUrl(string key)
        {
            var bucket = _configuration[AwsLocationNames.S3DocumentsBucket];
            var region = _configuration[AwsLocationNames.AwsRegion] ?? "ap-south-1";
            return $"https://{bucket}.s3.{region}.amazonaws.com/{Uri.EscapeDataString(key)}";
        }
    }
} 