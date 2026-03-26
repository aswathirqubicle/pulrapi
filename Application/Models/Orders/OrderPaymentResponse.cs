namespace Core.Application.Models.Orders
{
    public class OrderPaymentResponse
    {
        public int? Last4 { get; set; }
        public string CardType { get; set; }
    }
}

