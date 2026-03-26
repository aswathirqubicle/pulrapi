using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class StoryResponse
    {
        public string Uid { get; set; }
        public string Text { get; set; }
        public string DisplayName { get; set; }
        public int LikesCount { get; set; }
        public bool LikedByMe { get; set; }
        public bool SeenByMe { get; set; }
        public MediaFileDetailsResponse MediaFile { get; set; }
        public IEnumerable<ProductTagCoordinatesResponse> TaggedProducts { get; set; }
        public bool PostedByStore { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; internal set; }
        public string EntityUid { get; internal set; }
        public SharedPostPreviewDto SharedPostPreview { get; set; } // Preview info for shared post
        public StoryTypeEnum StoryType { get; set; }
        public SharedProductPreviewDto SharedProductPreview { get; set; }
        public SharedCollectionPreviewDto SharedCollectionPreview { get; set; }
        public List<string> Colors { get; set; } = new List<string>();
        public int? VideoWidth { get; set; }
        public int? VideoHeight { get; set; }
    }
}
