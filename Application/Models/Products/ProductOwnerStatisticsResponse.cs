using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class ProductOwnerStatisticsListResponse
    {
        public List<ProductOwnerStatisticsResponse> Owners { get; set; } = new List<ProductOwnerStatisticsResponse>();
    }

    public class ProductOwnerStatisticsResponse
    {
        public string OwnerUserId { get; set; }
        public string OwnerUsername { get; set; }
        public string OwnerDisplayName { get; set; }
        public string OwnerEmail { get; set; }
        public int TotalProducts { get; set; }
        public int TotalClicks { get; set; }
        public double AverageClicks { get; set; }
        public List<ProductClickSummary> Products { get; set; } = new List<ProductClickSummary>();
    }

    public class ProductClickSummary
    {
        public string ProductUid { get; set; }
        public string ProductName { get; set; }
        public int ClickCount { get; set; }
    }
}

