using System.Collections.Generic;
using Core.Application.Models.BagItems;

namespace Core.Application.Models.BagItems
{
    public class BagItemsExchangeRatesResponse
    {
        public List<BagProductExchangeRateDto> Products { get; set; } = new List<BagProductExchangeRateDto>();
    }
}
