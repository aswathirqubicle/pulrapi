using Core.Domain.Entities;

namespace Core.Application.Models.Onboarding
{
    public class OnboardingPreferenceResponse
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public string Gender { get; set; }
    }
}