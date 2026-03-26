using System;
using System.Collections.Generic;

namespace Application.DTOs.Search;

public class PaginatedResultDto<T>
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasMore { get; set; }
    public List<T> Data { get; set; } = new();

    public static PaginatedResultDto<T> Create(int currentPage, int pageSize, int totalCount, List<T> data)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PaginatedResultDto<T>
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasMore = currentPage < totalPages,
            Data = data
        };
    }
}