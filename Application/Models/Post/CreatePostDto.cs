using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Models.Post;
using Core.Application.Security.Validation.Attributes;

using Microsoft.AspNetCore.Http;

namespace Core.Application.Models.Post
{
    public class CreatePostDto
    {
        [SafeUid(allowNullValue: true, ErrorMessage = "Store UID contains invalid characters or format.")]
        public string StoreUid { get; set; }
        public string Text { get; set; }
        public string ImgDescription { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Mentions { get; set; } = new List<string>();
        public double SpotExpiryHours { get; set; } = 0;
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
        public string Location { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public int? VideoWidth { get; set; }
        public int? VideoHeight { get; set; }
    }
}
