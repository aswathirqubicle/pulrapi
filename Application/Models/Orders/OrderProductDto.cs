namespace Core.Application.Models.Orders;

public class OrderProductDto
{
    public string Uid { get; set; }
    public int BagQuantity { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; }
    public string StoreUid { get; set; }
    public string AffiliateId { get; set; }
    public string? VariantCombinationUid { get; set; }
}
