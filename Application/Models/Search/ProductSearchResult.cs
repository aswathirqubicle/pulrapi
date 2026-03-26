namespace Core.Application.Models.Search;

public class ProductSearchResult
{
    public string ProductUid { get; set; }
    public string ProductImageUrl { get; set; }
    public string ProductName { get; set; }
    public string WhatIsIt { get; set; }
    public string Brand { get; set; }
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
    public string CountryCode { get; set; }
    public string CurrencyCode { get; set; }
    public bool IsDeletable { get; set; } = true;
}
