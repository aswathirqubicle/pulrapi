using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class ProfileWithStoriesResponse
    {
        public ProfileForStoryResponse Profile { get; internal set; }
        public IEnumerable<StoryResponse> Stories { get; internal set; }
    }
}
