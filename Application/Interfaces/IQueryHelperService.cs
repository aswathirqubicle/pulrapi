using System;
using System.Linq;

namespace Core.Application.Interfaces
{
    public interface IQueryHelperService
    {
        IQueryable<TEntity> AppendOrderBy<TEntity>(IQueryable<TEntity> entityQuery, string orderBy, string order) where TEntity : class;
    }
}
