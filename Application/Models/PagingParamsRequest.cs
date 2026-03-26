using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models
{
    public class PagingParamsRequest 
    {
        const int maxPageSize = 100;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value > maxPageSize) ? maxPageSize : value;
            }
        }

        [StringLengthCheck(MinStringLength = 2, MaxStringLength = 150)]
        public string Search { get; set; }

        public string OrderBy { get; set; }

        [QueryOrder]
        public string Order { get; set; }
    }
}
