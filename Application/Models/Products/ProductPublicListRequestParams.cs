
using Core.Application.Models;


namespace Core.Application.Models.Products
{
    public class ProductPublicListRequestParams : PagingParamsRequest
    {
        public string StoreUid { get; set; }
        public string CategoryUid { get; set; }
        public string PostUid { get; set; }
        public string CurrencyCode { get; set; }
    }
}
