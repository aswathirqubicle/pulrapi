
using Core.Domain.Enums;

namespace Core.Application.Models.Fulfillments
{
    public class FulfillmentDetailsResponse
    {
        public string Uid { get; set; }
        public string StoreUid { get; set; }
        public string PickupCity{ get; set; }
        public string PickupPlaceNumber { get; set; }
        public string PickupAddress { get; set; }
        public FulfillmentMethodEnum? FulfillmentMethod { get; set; }
        public string ReturnCity { get; set; }
        public string ReturnPlaceNumber { get; set; }
        public string ReturnAddress { get; set; }
        public string PickupCountryUid { get; set; }
        public string ReturnCountryUid { get; set; }
    }
}
