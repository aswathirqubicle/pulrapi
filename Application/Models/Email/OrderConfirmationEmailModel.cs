using System.Collections.Generic;

namespace Core.Application.Models.Email
{
    public class OrderConfirmationEmailModel
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal EstimatedVAT { get; set; }
        public decimal ShippingFee { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string DeliveryAddress { get; set; }
        public string PhoneNumber { get; set; }
        public List<OrderProductEmailModel> Products { get; set; } = new();
    }

    public class OrderProductEmailModel
    {
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string VariantDetails { get; set; }
    }

    public class BuyerOrderConfirmationEmailModel : OrderConfirmationEmailModel
    {
        public string OrderSummaryUrl { get; set; }
    }

    public class SellerOrderNotificationEmailModel
    {
        public string SellerName { get; set; }
        public string SellerEmail { get; set; }
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal EstimatedVAT { get; set; }
        public decimal ShippingFee { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string DeliveryAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string OrdersAreaUrl { get; set; }
        public List<OrderProductEmailModel> Products { get; set; } = new();
    }

    public class BuyerOrderShippedEmailModel
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public string ShippedOn { get; set; }
        public string TrackingNumber { get; set; }
        public string DeliveryService { get; set; }
        public string DeliveryAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string OrderSummaryUrl { get; set; }
        public List<OrderProductEmailModel> Products { get; set; } = new();
    }
}
