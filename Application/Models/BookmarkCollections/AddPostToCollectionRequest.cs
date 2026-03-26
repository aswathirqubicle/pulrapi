using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.BookmarkCollections
{
    public class AddPostToCollectionRequest
    {
        [SafeUid(allowNullValue: false, maxLength: 50, minLength: 1, ErrorMessage = "Post UID contains invalid characters or format.")]
        public string PostUid { get; set; }
        
        [SafeUid(allowNullValue: false, maxLength: 50, minLength: 1, ErrorMessage = "Collection UID contains invalid characters or format.")]
        public string CollectionUid { get; set; }
    }
}
