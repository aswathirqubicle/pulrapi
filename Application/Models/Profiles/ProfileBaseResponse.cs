using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Core.Application.Models.Profiles
{
    public class ProfileBaseResponse
    {
        public string Uid { get; set; }
        public string UserId { get; set; }
        public string ImageUrl { get; set; }
        //public bool IsStore { get; set; }
        public bool IsInfluencer { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string UserType { get; set; }
        public bool FollowedByMe { get; set; }

        //Temporary field for batch influencer check
        [JsonIgnore] // Don't send this to API response
        public string _UserId { get; set; } // This will store User.Id
    }
}
