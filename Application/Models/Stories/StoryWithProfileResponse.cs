namespace Core.Application.Models.Stories
{
    public class StoryWithProfileResponse
    {
        public StoryResponse Story { get; set; }
        public ProfileForStoryResponse Profile { get; set; }
    }
}
