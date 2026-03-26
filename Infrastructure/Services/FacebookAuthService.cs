using Core.Application.Models.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.External.Facebook;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class FacebookAuthService : IFacebookAuthService
    {
        private const string TokenValidationUrl = "https://graph.facebook.com/debug_token?input_token={0}&access_token={1}|{2}";
        private const string UserInfoUrl = "https://graph.facebook.com/me?fields=first_name,last_name,picture,email&access_token={0}";
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<FacebookAuthService> _logger;
        private readonly IConfiguration _configuration;

        public FacebookAuthService(IHttpClientService httpClientService, 
            ILogger<FacebookAuthService> logger,
            IConfiguration configuration)
        {
            _httpClientService = httpClientService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<FacebookTokenValidationResult> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                var formattedUrl = string.Format(TokenValidationUrl, accessToken, _configuration["FacebookAuth:AppId"], _configuration["FacebookAuth:AppSecret"]);

                var result = await _httpClientService.CreateRequest(HttpMethod.Get, formattedUrl, null, null);
                // will throw exception if bad status code
                result.EnsureSuccessStatusCode();

                var responseAsString = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FacebookTokenValidationResult>(responseAsString);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<FacebookUserInfoResult> GetUserInfoAsync(string accessToken)
        {
            try
            {
                var formattedUrl = string.Format(UserInfoUrl, accessToken);
                var result = await _httpClientService.CreateRequest(HttpMethod.Get, formattedUrl, null, null);
                // will throw exception if bad status code
                result.EnsureSuccessStatusCode();

                var responseAsString = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FacebookUserInfoResult>(responseAsString);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

    }
}
