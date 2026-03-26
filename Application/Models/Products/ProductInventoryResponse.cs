namespace Core.Application.Models.Products
{
    public class ProductInventoryResponse
    {
        public string Uid { get; set; }
        public string ArticleCode { get; set; }
        public string Name { get; set; }
        public string CategoryTitle { get; set; }
        public double Price { get; set; }
        public string CurrencyCode { get; set; }
        public string ImageUrl { get; set; }
    }
}
