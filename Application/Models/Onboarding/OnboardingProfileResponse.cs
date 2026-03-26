namespace Core.Application.Models.Onboarding
{
    public class OnboardingProfileResponse
    {
        public string Uid { get; set; }
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string UserType { get; set; }
        public string About { get; set; }
        public bool IsInfluencer { get; set; }
        public string ImageUrl { get; internal set; }
        public int Followers { get; set; }
    }
}
