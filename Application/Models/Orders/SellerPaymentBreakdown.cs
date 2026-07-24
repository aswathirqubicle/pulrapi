using System.Collections.Generic;

namespace Core.Application.Models.Orders
{
    public class SellerPaymentBreakdown
    {
        public string SellerName { get; set; }
        public string SellerProfileUid { get; set; }
        public decimal? Subtotal { get; set; }    // seller view only
        public decimal Shipping { get; set; }     // always shown
        public decimal Total { get; set; }
        public List<OrderItemBreakdown> Items { get; set; }
    }
}
