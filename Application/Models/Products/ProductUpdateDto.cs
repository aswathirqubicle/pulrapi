using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Application.Models.Products;
using Core.Domain.Enums;

namespace Core.Application.Models.Products
{
    public class ProductUpdateDto
    {
        [Required]
        public string Uid { get; set; }
        public ProductSellTypeEnum SellType { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string CategoryUid { get; set; }
        public string ArticleCode { get; set; }
        public ICollection<ProductMoreInfoUpdateRequest> MoreInfos { get; set; } = new List<ProductMoreInfoUpdateRequest>();
        public ICollection<ProductAttributeUpdateRequest> ProductAttributes { get; set; } = new List<ProductAttributeUpdateRequest>();
        public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
        public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
    }
}
