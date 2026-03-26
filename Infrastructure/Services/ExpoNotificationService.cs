using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    public class ExpoNotificationService : IExpoNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExpoNotificationService> _logger;
        private readonly string _expoApiUrl = "https://exp.host/--/api/v2/push/send";

        public ExpoNotificationService(HttpClient httpClient, ILogger<ExpoNotificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string expoToken, string title, string body, object data = null)
        {
            try
            {
                var message = new ExpoMessage
                {
                    To = expoToken,
                    Title = title,
                    Body = body,
                    Data = data,
                    Sound = "default",
                    Priority = "high"
                };

                var messages = new List<ExpoMessage> { message };
                await SendMessagesAsync(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to token: {Token}", expoToken);
                throw;
            }
        }

        public async Task SendNotificationsAsync(List<string> expoTokens, string title, string body, object data = null)
        {
            var batchcount = data.GetType().GetProperty("batchCount")?.GetValue(data, null) as int? ?? 1;
            try
            {
                var messages = new List<ExpoMessage>();
                foreach (var token in expoTokens)
                {
                    messages.Add(new ExpoMessage
                    {
                        To = token,
                        Title = title,
                        Body = body,
                        Data = data,
                        Sound = "default",
                        Priority = "high",
                        Badge = batchcount > 0 ? batchcount : null,
                    });
                }

                await SendMessagesAsync(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending notifications to {expoTokens.Count} tokens", expoTokens.Count);
                throw;
            }
        }

        public bool ValidateToken(string expoToken)
        {
            try
            {
                // Basic validation - Expo tokens should start with ExponentPushToken[ or ExpoPushToken[
                return expoToken.StartsWith("ExponentPushToken[") || expoToken.StartsWith("ExpoPushToken[");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token: {Token}", expoToken);
                return false;
            }
        }

        private async Task SendMessagesAsync(List<ExpoMessage> messages)
        {
            try
            {
                _logger.LogInformation("Sending {MessageCount} messages to Expo API", messages.Count);
                
                var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("Accept-encoding", "gzip, deflate");

                var response = await _httpClient.PostAsync(_expoApiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Expo API error: {StatusCode} - {Content}. Messages: {MessageCount}", response.StatusCode, errorContent, messages.Count);
                    throw new Exception($"Expo API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Expo notification sent successfully. Response: {Response}. Messages: {MessageCount}", responseContent, messages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {MessageCount} messages to Expo API. Error: {ErrorMessage}", messages.Count, ex.Message);
                throw;
            }
        }

    }

    public class ExpoMessage
    {
        public string To { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public object Data { get; set; }
        public string Sound { get; set; }
        public string Priority { get; set; }
        public int? Badge { get; set; }
        public int? Ttl { get; set; } = 86400; // 24 hours
    }
} 