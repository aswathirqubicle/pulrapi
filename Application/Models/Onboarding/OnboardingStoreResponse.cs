namespace Core.Application.Models.Onboarding
{
    public class OnboardingStoreResponse
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; internal set; }
    }
}
