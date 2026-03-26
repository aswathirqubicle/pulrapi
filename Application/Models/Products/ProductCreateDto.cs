using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Models.Products;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;

namespace Core.Application.Models.Products
{
    public class ProductCreateDto
    {
        [SafeUid(allowNullValue: false, ErrorMessage = "Store UID contains invalid characters or format.")]
        public string StoreUid { get; set; }
        public ProductSellTypeEnum SellType { get; set; }

        public string Name { get; set; }
        
        [Required(ErrorMessage = "Price is required.")]
        public double Price { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; } 
        
        [SafeUid(allowNullValue: true, ErrorMessage = "Category UID contains invalid characters or format.")]
        public string CategoryUid { get; set; }
        public ICollection<ProductMoreInfoRequest> MoreInfos { get; set; } = new List<ProductMoreInfoRequest>();
        public ICollection<ProductVariantCreateRequest> ProductAttributes { get; set; }
        public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
        public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
    }
}
