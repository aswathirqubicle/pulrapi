using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class Fulfillment : EntityBase
    {
        [Required]
        public int StoreId { get; set; }
        public Store Store { get; set; }
        public FulfillmentMethodEnum FulfillmentMethod { get; set; }
        public string PickupAddress { get; set; }
        public string PickupPlaceNumber { get; set; }
        public Country PickupCountry { get; set; }
        public string PickupCity { get; set; }
        public string ReturnAddress { get; set; }
        public string ReturnPlaceNumber { get; set; }
        public Country ReturnCountry { get; set; }
        public string ReturnCity { get; set; }
    }
}
