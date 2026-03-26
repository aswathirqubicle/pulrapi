namespace Core.Application.Models.Stores
{
    public class StoreBaseResponse
    {
        public string Uid { get; set; }
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public bool FollowedByMe { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsMyStore { get; set; } = false;
    }
}
