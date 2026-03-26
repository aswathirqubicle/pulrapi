using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    public class ProductClickStatisticsResponse
    {
        public string ProductUid { get; set; }
        public string ProductName { get; set; }
        public int TotalClicks { get; set; }
        public List<ProductClickUserDetail> ClickedUsers { get; set; } = new List<ProductClickUserDetail>();
    }

    public class ProductClickUserDetail
    {
        public string UserUid { get; set; }
        public string UserName { get; set; }
        public int ClickCount { get; set; }
    }
}

