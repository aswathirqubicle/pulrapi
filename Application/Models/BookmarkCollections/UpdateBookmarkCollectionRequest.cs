using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.BookmarkCollections
{
    public class UpdateBookmarkCollectionRequest
    {
        [SafeUid(allowNullValue: false, ErrorMessage = "Collection UID contains invalid characters or format.")]
        public string Uid { get; set; }
        
        [SafeName(allowNullValue: false, maxLength: 100, minLength: 1, ErrorMessage = "Collection name contains invalid characters or format.")]
        public string Name { get; set; }
    }
} 