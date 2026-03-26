
using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.Categories
{
    public class CategoryCreateDto
    {
        [SafeName(allowNullValue: false, maxLength: 100, minLength: 1, ErrorMessage = "Category title contains invalid characters or format.")]
        public string Title { get; set; }
        
        [SafeUid(allowNullValue: false, ErrorMessage = "Store UID contains invalid characters or format.")]
        public string StoreUid { get; set; }
        
        [SafeUid(allowNullValue: true, ErrorMessage = "Parent category UID contains invalid characters or format.")]
        public string ParentCategoryUid { get; internal set; }
    }
}
