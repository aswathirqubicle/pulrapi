
using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class ProductCategoryResponse
    {
        public string CategoryUid { get; set; }
        public string CategoryName { get; set; }
        public string StoreUid { get; set; }
        public List<string> ProductUids { get; set; }
    }
}
