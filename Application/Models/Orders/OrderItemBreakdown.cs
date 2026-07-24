namespace Core.Application.Models.Orders
{
    public class OrderItemBreakdown
    {
        public string ProductUid { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Shipping { get; set; }
    }
}
