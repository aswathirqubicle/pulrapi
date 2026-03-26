using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.BookmarkCollections
{
    public class CreateBookmarkCollectionRequest
    {
        [SafeName(allowNullValue: false, maxLength: 100, minLength: 1, ErrorMessage = "Collection name contains invalid characters or format.")]
        public string Name { get; set; }
        
        [SafeUid(allowNullValue: true, maxLength: 50, minLength: 1, ErrorMessage = "Post ID contains invalid characters or format.")]
        public string PostId { get; set; } // Optional: add post to collection on creation
    }
} 