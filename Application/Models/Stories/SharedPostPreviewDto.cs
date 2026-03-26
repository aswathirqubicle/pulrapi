using System;

namespace Core.Application.Models.Stories
{
    public class SharedPostPreviewDto
    {
        public string PostUid { get; set; }
        public string PostOwnerUserName { get; set; }
        public string ContentPreview { get; set; }
        public string PostImageUrl { get; set; }
        public string PostOwnerImageUrl { get; set; }
    }
} 