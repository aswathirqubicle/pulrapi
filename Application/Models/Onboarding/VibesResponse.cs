using System.Collections.Generic;

namespace Core.Application.Models.Onboarding
{
    public class VibesResponse
    {
        public List<VibeCategoryResponse> Categories { get; set; } = new List<VibeCategoryResponse>();
    }

    public class VibeCategoryResponse
    {
        public string CategoryName { get; set; }
        public List<VibeResponse> Vibes { get; set; } = new List<VibeResponse>();
    }
}
