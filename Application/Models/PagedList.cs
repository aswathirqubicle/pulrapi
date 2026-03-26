using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Models
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }

        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;


        public PagedList(List<T> items, int count = 10, int pageNumber = 1, int pageSize = 9)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public static async Task<PagedList<T>> ToPagedListAsync(IQueryable<T> source, int pageNumber, int pageSize, int? externalCount = null)
        {
            var count =  externalCount ?? await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        public static PagedList<T> ToPagedList(List<T> source, int pageNumber, int pageSize, int? externalCount = null)
        {
            var count = externalCount ?? source.Count();
            var items = new List<T>();
            if (externalCount != null) {
                items = source.ToList();
            }
            else
            {
                items = source.Skip((pageNumber - 1) * pageSize)
                                              .Take(pageSize)
                                              .ToList();
            }

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
