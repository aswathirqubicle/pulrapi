using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;
        private readonly HttpClient _httpClient;

        public GoogleAuthService(
            IConfiguration configuration,
            ILogger<GoogleAuthService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken)
        {
            try
            {
                // First try the userinfo endpoint which provides complete user information
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User info response: {content}");
                    
                    var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content);
                    
                    if (!string.IsNullOrEmpty(userInfo.Email))
                    {
                        _logger.LogInformation($"User info retrieved from userinfo endpoint - Email: {userInfo.Email}, Name: {userInfo.Given_Name} {userInfo.Family_Name}");
                        return userInfo;
                    }
                    
                    _logger.LogWarning("Email not found in userinfo response");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Userinfo endpoint failed. Status: {response.StatusCode}");
                    _logger.LogWarning($"Error content: {errorContent}");
                }

                // If userinfo endpoint fails, try the token info endpoint for basic info
                _logger.LogInformation("Trying to get user info from token info endpoint...");
                response = await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={accessToken}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Token info response: {content}");
                    
                    var tokenInfo = JsonSerializer.Deserialize<JsonElement>(content);
                    var userInfo = new GoogleUserInfo();
                    
                    if (tokenInfo.TryGetProperty("email", out var emailProperty))
                    {
                        userInfo.Email = emailProperty.GetString();
                        _logger.LogInformation($"Found email in token info: {userInfo.Email}");
                    }
                    
                    // Try to get additional user info from the userinfo endpoint with the same token
                    try
                    {
                        var userInfoResponse = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}");
                        if (userInfoResponse.IsSuccessStatusCode)
                        {
                            var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Additional user info response: {userInfoContent}");
                            
                            var additionalInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoContent);
                            userInfo.Given_Name = additionalInfo.Given_Name;
                            userInfo.Family_Name = additionalInfo.Family_Name;
                            userInfo.Name = additionalInfo.Name;
                            userInfo.Picture = additionalInfo.Picture;
                            
                            _logger.LogInformation($"Retrieved additional user info - Name: {userInfo.Given_Name} {userInfo.Family_Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to get additional user info: {ex.Message}");
                    }
                    
                    if (!string.IsNullOrEmpty(userInfo.Email))
                    {
                        _logger.LogInformation($"User info retrieved from token info - Email: {userInfo.Email}, Name: {userInfo.Given_Name} {userInfo.Family_Name}");
                        return userInfo;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Token info endpoint failed. Status: {response.StatusCode}");
                    _logger.LogError($"Error content: {errorContent}");
                }

                _logger.LogError("Failed to get email from both endpoints");
                throw new Exception("Email is required but was not provided by Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Google user info");
                throw;
            }
        }

        private async Task<string> GetIdTokenAsync(string accessToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/tokeninfo?access_token={accessToken}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Token info response: {content}");
                    return content;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ID token info");
                return null;
            }
        }

        private string ExtractEmailFromIdToken(string idToken)
        {
            try
            {
                var tokenData = JsonSerializer.Deserialize<JsonElement>(idToken);
                if (tokenData.TryGetProperty("email", out var emailProperty))
                {
                    return emailProperty.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email from ID token");
                return null;
            }
        }
    }
} 