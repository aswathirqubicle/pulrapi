using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Application.Models.External.Apple
{
    public class AppleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("is_private_email")]
        public bool IsPrivateEmail { get; set; }

        [JsonPropertyName("real_user_status")]
        public int RealUserStatus { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("authorizationCode")]
        public string AuthorizationCode { get; set; }

        [JsonPropertyName("fullName")]
        public AppleNameInfo NameInfo { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
    }

    public class AppleNameInfo
    {
        [JsonPropertyName("familyName")]
        public string FamilyName { get; set; }

        [JsonPropertyName("givenName")]
        public string GivenName { get; set; }

        [JsonPropertyName("middleName")]
        public string MiddleName { get; set; }

        [JsonPropertyName("namePrefix")]
        public string NamePrefix { get; set; }

        [JsonPropertyName("nameSuffix")]
        public string NameSuffix { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
    }
}
