using System.Collections.Generic;
using Core.Application.Models.Currencies;
using Core.Application.Models.Stores;

namespace Core.Application.Models.Stores
{
    public class StoreSetupDataResponse
    {
        public List<IndustryResponse> Industries { get; set; }
        public List<CurrencyDetailsResponse> Currencies { get; set; }
    }
}
