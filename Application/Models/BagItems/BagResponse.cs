using System.Collections.Generic;
using Core.Application.Models.BagItems;
using Core.Application.Models.Currencies;

namespace Core.Application.Models.BagItems
{
    public class BagResponse
    {
        public List<BagProductResponse> Products { get; set; } = new List<BagProductResponse>();
        public CurrencyDetailsResponse Currency { get; set; }
        public decimal? TotalShippingCost { get; set; } = 0;
    }
}
