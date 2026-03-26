using System.Collections.Generic;
using Core.Application.Models.Post;

namespace Core.Application.Models.BookmarkCollections
{
    public class BookmarkCollectionResponse
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public int PostsCount { get; set; }
        public List<PostResponse> Items { get; set; }
        public List<string> PreviewImages { get; set; } // First 4 post image URLs
        public List<string> PostUids { get; set; } // All post UIDs in the collection
    }
} 