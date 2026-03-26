using Core.Application.Interfaces;
using Core.Application.Models.External.Apple;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace Core.Infrastructure.Services
{
    public class AppleAuthService : IAppleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AppleAuthService> _logger;
        private readonly IMemoryCache _cache;
        private const string APPLE_KEYS_CACHE_KEY = "apple_public_keys";
        private const int CACHE_DURATION_HOURS = 24;
        private static readonly SemaphoreSlim _keyFetchSemaphore = new SemaphoreSlim(1, 1);

        public AppleAuthService(IConfiguration configuration, HttpClient httpClient, ILogger<AppleAuthService> logger, IMemoryCache cache)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
        }

        public Task<AppleUserInfo> GetUserInfoAsync(string identityToken, string fullResponse = null)
        {
            try
            {
                if (!IsValidJwtFormat(identityToken))
                {
                    throw new SecurityTokenMalformedException("Invalid token format. The token needs to be in JWS Compact Serialization Format.");
                }

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(identityToken);

                // Extract claims from token

                AppleUserInfo userInfo;

                if (!string.IsNullOrEmpty(fullResponse))
                {
                    try
                    {
                        userInfo = JsonSerializer.Deserialize<AppleUserInfo>(fullResponse);
                    }
                    catch (Exception)
                    {
                        userInfo = new AppleUserInfo();
                    }
                }
                else
                {
                    userInfo = new AppleUserInfo();
                }

                userInfo.Sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                userInfo.Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                userInfo.EmailVerified = token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";
                userInfo.IsPrivateEmail = token.Claims.FirstOrDefault(c => c.Type == "is_private_email")?.Value == "true";
                userInfo.RealUserStatus = int.Parse(token.Claims.FirstOrDefault(c => c.Type == "real_user_status")?.Value ?? "0");
                userInfo.User = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                var nameHeader = token.Header.FirstOrDefault(h => h.Key == "name");
                if (nameHeader.Value != null)
                {
                    try
                    {
                        var nameData = JsonSerializer.Deserialize<AppleNameInfo>(nameHeader.Value.ToString());
                        if (IsValidNameInfo(nameData))
                        {
                            userInfo.NameInfo = nameData;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore parsing errors
                    }
                }

                if (userInfo.NameInfo == null)
                {
                    var nameClaim = token.Claims.FirstOrDefault(c => c.Type == "name");
                    if (nameClaim != null)
                    {
                        try
                        {
                            var nameData = JsonSerializer.Deserialize<AppleNameInfo>(nameClaim.Value);
                            if (IsValidNameInfo(nameData))
                            {
                                userInfo.NameInfo = nameData;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore parsing errors
                        }
                    }
                }

                if (userInfo.NameInfo == null)
                {
                    try
                    {
                        var payload = token.Payload;
                        if (payload.ContainsKey("name"))
                        {
                            var nameData = JsonSerializer.Deserialize<AppleNameInfo>(payload["name"].ToString());
                            if (IsValidNameInfo(nameData))
                            {
                                userInfo.NameInfo = nameData;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore parsing errors
                    }
                }

                return Task.FromResult(userInfo);
            }
            catch (SecurityTokenMalformedException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> ValidateIdentityTokenAsync(string identityToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(identityToken);

                var keys = await GetApplePublicKeysAsync();
                if (keys == null || !keys.Keys.Any())
                {
                    return false;
                }

                var keyId = token.Header.Kid;

                var key = keys.Keys.FirstOrDefault(k => k.Kid == keyId);
                if (key == null)
                {
                    // Clear cache and retry once
                    _cache.Remove(APPLE_KEYS_CACHE_KEY);
                    keys = await GetApplePublicKeysAsync();
                    key = keys?.Keys?.FirstOrDefault(k => k.Kid == keyId);
                    
                    if (key == null)
                    {
                        return false;
                    }
                }

                // Create RSA key with proper disposal handling
                var rsaKey = CreateRsaSecurityKey(key);
                if (rsaKey == null)
                {
                    return false;
                }

                var clientId = _configuration["AppleAuth:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                {
                    return false;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaKey,
                    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = true,
                    ValidAudience = clientId,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                var principal = handler.ValidateToken(identityToken, validationParameters, out _);
                return principal != null;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return false;
            }
            catch (SecurityTokenExpiredException)
            {
                return false;
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                return false;
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsValidNameInfo(AppleNameInfo nameInfo)
        {
            if (nameInfo == null)
                return false;

            // Check if any name property contains "String" (common serialization artifact)
            var properties = new[] { nameInfo.GivenName, nameInfo.FamilyName, nameInfo.MiddleName, nameInfo.NamePrefix, nameInfo.NameSuffix, nameInfo.Nickname };
            
            foreach (var prop in properties)
            {
                if (!string.IsNullOrWhiteSpace(prop) && prop.Equals("String", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Return true if at least one valid name property exists
            return !string.IsNullOrWhiteSpace(nameInfo.GivenName) || !string.IsNullOrWhiteSpace(nameInfo.FamilyName);
        }

        private RsaSecurityKey CreateRsaSecurityKey(AppleKey key)
        {
            try
            {
                var modulusBytes = Base64UrlDecode(key.N);
                var exponentBytes = Base64UrlDecode(key.E);

                // Create RSA parameters without using statement to avoid premature disposal
                var rsaParams = new RSAParameters
                {
                    Modulus = modulusBytes,
                    Exponent = exponentBytes
                };

                // Create a new RSA instance that will be owned by RsaSecurityKey
                var rsa = RSA.Create();
                rsa.ImportParameters(rsaParams);

                // RsaSecurityKey will manage the RSA instance lifecycle
                var rsaKey = new RsaSecurityKey(rsa) { KeyId = key.Kid };

                return rsaKey;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 0: break;
                case 2: s += "=="; break;
                case 3: s += "="; break;
                default: throw new FormatException("Invalid Base64Url string.");
            }
            return Convert.FromBase64String(s);
        }

        private static bool IsValidJwtFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    return false;
            }

            try
            {
                var headerBytes = Base64UrlDecode(parts[0]);
                var headerJson = Encoding.UTF8.GetString(headerBytes);
                
                var payloadBytes = Base64UrlDecode(parts[1]);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);

                if (!IsValidJson(headerJson) || !IsValidJson(payloadJson))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidJson(string json)
        {
            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<AppleKeysResponse> GetApplePublicKeysAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(APPLE_KEYS_CACHE_KEY, out AppleKeysResponse cachedKeys))
            {
                if (cachedKeys?.Keys != null && cachedKeys.Keys.Any())
                {
                    return cachedKeys;
                }
            }

            // Use semaphore to prevent multiple simultaneous fetches
            await _keyFetchSemaphore.WaitAsync();
            try
            {
                // Double-check cache after acquiring lock
                if (_cache.TryGetValue(APPLE_KEYS_CACHE_KEY, out cachedKeys))
                {
                    if (cachedKeys?.Keys != null && cachedKeys.Keys.Any())
                    {
                        return cachedKeys;
                    }
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var response = await _httpClient.GetAsync("https://appleid.apple.com/auth/keys", cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var keyJson = await response.Content.ReadAsStringAsync();
                var keys = JsonSerializer.Deserialize<AppleKeysResponse>(keyJson);

                if (keys?.Keys == null || !keys.Keys.Any())
                {
                    return null;
                }

                // Validate keys
                var validKeys = keys.Keys.Where(k =>
                    !string.IsNullOrEmpty(k.Kid) &&
                    !string.IsNullOrEmpty(k.N) &&
                    !string.IsNullOrEmpty(k.E) &&
                    k.Alg == "RS256" &&
                    k.Use == "sig"
                ).ToList();

                if (!validKeys.Any())
                {
                    return null;
                }

                var cleanKeysResponse = new AppleKeysResponse { Keys = validKeys };

                // Cache with absolute expiration
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(CACHE_DURATION_HOURS))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(APPLE_KEYS_CACHE_KEY, cleanKeysResponse, cacheOptions);

                return cleanKeysResponse;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                _keyFetchSemaphore.Release();
            }
        }

    }
}

public class AppleKeysResponse
{
    [JsonPropertyName("keys")]
    public List<AppleKey> Keys { get; set; }
}

public class AppleKey
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; }

    [JsonPropertyName("kid")]
    public string Kid { get; set; }

    [JsonPropertyName("use")]
    public string Use { get; set; }

    [JsonPropertyName("alg")]
    public string Alg { get; set; }

    [JsonPropertyName("n")]
    public string N { get; set; }

    [JsonPropertyName("e")]
    public string E { get; set; }
}