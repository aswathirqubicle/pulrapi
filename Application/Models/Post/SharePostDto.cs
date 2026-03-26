using System.Collections.Generic;

namespace Core.Application.Models.Post
{
    public class SharePostDto
    {
        public string StoreUid { get; set; }
        public string Text { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Mentions { get; set; } = new List<string>();
        public double SpotExpiryHours { get; set; } = 0;
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
        public string SharedPostUid { get; set; }
    }
}
