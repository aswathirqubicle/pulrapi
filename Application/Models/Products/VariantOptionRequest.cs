using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Products
{
    /// <summary>
    /// Request model for creating a variant option (e.g., Size, Color, Material)
    /// </summary>
    public class VariantOptionRequest
    {
        [Required(ErrorMessage = "Option name is required")]
        [MaxLength(100, ErrorMessage = "Option name cannot exceed 100 characters")]
        public string OptionName { get; set; } // e.g., "Size", "Color", "Material"

        [Required(ErrorMessage = "At least one option value is required")]
        [MinLength(1, ErrorMessage = "At least one option value is required")]
        public List<string> Values { get; set; } = new List<string>(); // e.g., ["Small", "Medium", "Large"]
    }
}
