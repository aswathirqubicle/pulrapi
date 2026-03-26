using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class SharedCollectionPreviewDto
    {
        public string CollectionUid { get; set; }
        public string OwnerUsername { get; set; }
        public string OwnerProfileImageUrl { get; set; }
        public string CollectionName { get; set; }
        public int TotalPostCount { get; set; }
        public List<string> First4PostImageUrls { get; set; } = new List<string>();
    }
}


