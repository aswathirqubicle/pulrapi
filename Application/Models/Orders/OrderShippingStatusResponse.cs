using System;
using System.Collections.Generic;

namespace Core.Application.Models.Orders
{
    public class ItemShippingStatusResponse
    {
        public string OrderUid { get; set; }
        public string ItemUid { get; set; }
        public string ProductName { get; set; }
        public string PrimaryImageUrl { get; set; }
        public bool IsShipped { get; set; }
        public string TrackingNumber { get; set; }
        public string ShippingProvider { get; set; }
        public DateTime? ShippedAt { get; set; }
        public List<string> ShippingProofMediaFileURLs { get; set; }
    }
}
