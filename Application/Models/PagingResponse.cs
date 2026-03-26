using System;
using System.Collections.Generic;

namespace Core.Application.Models
{
    public class PagingResponse<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public List<T> Items { get; set; } = new List<T>();

        public List<string> ItemIds { get; set; } = new List<string>();

        public int CurrentUserBagItemsCount { get; set; }
        public int CurrentUserWishlistCount { get; set; }
        public int CurrentUserBagItemsTotalQuantity { get; set; }
        public bool HasProcessingOrders { get; set; }

    }
}
