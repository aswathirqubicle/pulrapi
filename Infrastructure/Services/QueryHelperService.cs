using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class QueryHelperService : IQueryHelperService
    {
        private readonly IServiceProvider _serviceProvider;

        public QueryHelperService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQueryable<TEntity> AppendOrderBy<TEntity>(IQueryable<TEntity> entityQuery, string orderBy, string order) where TEntity : class
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueryHelperService>>();
                try
                {
                    if (!String.IsNullOrWhiteSpace(orderBy) && !String.IsNullOrWhiteSpace(order))
                    {
                        var isOrderASC = order == QueryConditions.OrderByASC;
                        var orderByProp = StringExtensions.FirstCharToUpper(orderBy);

                        var instance = (TEntity)Activator.CreateInstance(typeof(TEntity));
                        bool propExists = instance.GetType().GetProperty(orderByProp) != null;
                        // destroy instance:
                        instance = null;

                        if (propExists)
                        {
                            if (isOrderASC) { entityQuery = entityQuery.OrderBy(entity => EF.Property<object>(entity, orderByProp)); }
                            else { entityQuery = entityQuery.OrderByDescending(entity => EF.Property<object>(entity, orderByProp)); }
                        }
                        else
                        {
                            logger.LogError($"{nameof(TEntity)} doesn't have property '{orderByProp}' .");
                            throw new NotFoundException();
                        }
                    }

                    return entityQuery;
                }
                catch (Exception e)
                {
                    logger.LogError(e, e.Message);
                    throw;
                }
            }
        }
    }
}
