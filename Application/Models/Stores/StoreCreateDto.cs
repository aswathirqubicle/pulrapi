using System.ComponentModel.DataAnnotations;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.Stores
{
    public class StoreCreateDto
    {
        [SafeName(allowNullValue: false, maxLength: 100, minLength: 1, ErrorMessage = "Store name contains invalid characters or format.")]
        public string Name { get; set; }
        
        [SafeName(allowNullValue: false, maxLength: 200, minLength: 1, ErrorMessage = "Legal name contains invalid characters or format.")]
        public string LegalName { get; set; }
        
        [SafeName(allowNullValue: false, maxLength: 50, minLength: 1, ErrorMessage = "Unique name contains invalid characters or format.")]
        public string UniqueName { get; set; }
        
        [Required(ErrorMessage = "Store email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string StoreEmail { get; set; }
        
        [SafeUid(allowNullValue: false, ErrorMessage = "Currency UID contains invalid characters or format.")]
        public string CurrencyUid { get; set; }
    }
}
