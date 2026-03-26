using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Core.Application.Models.Users;

namespace Core.Application.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> GetUserInfoAsync(string accessToken);
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("given_name")]
        public string Given_Name { get; set; }

        [JsonPropertyName("family_name")]
        public string Family_Name { get; set; }

        [JsonPropertyName("picture")]
        public string Picture { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }
    }
} 