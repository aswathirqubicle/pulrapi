using System.Collections.Generic;

namespace Core.Application.Models.Search;

public class SearchResult
{
    public List<BaseSearchResult> Posts { get; set; } = new List<BaseSearchResult>();
    public List<ProfileSearchResult> Profiles { get; set; } = new List<ProfileSearchResult>();
    public List<ProductSearchResult> Products { get; set; } = new List<ProductSearchResult>();
    //public List<StoreSearchResult> Stores { get; set; } = new List<StoreSearchResult>();
}