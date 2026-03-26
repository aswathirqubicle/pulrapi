using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.ProductCategories.Queries
{
    public class GetCategoriesQuery : IRequest<List<ProductCategoryResponse>>
    {
        [Required]
        public string StoreUid { get; set; }
    }

    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<ProductCategoryResponse>>
    {
        private readonly ILogger<GetCategoriesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetCategoriesQueryHandler(ILogger<GetCategoriesQueryHandler> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public Task<List<ProductCategoryResponse>> Handle(GetCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                //TODO rewrite store categories 
                throw new NotImplementedException();
                /*return await _dbContext.ProductCategories.Where(c =>
                        c.Store.Uid == request.StoreUid)
                    .Select(c => new ProductCategoryResponse()
                    {
                        CategoryUid = c.Category.Uid,
                        CategoryName = c.Category.Name,
                        StoreUid = c.Store.Uid,
                        ProductUids = c.Products.Select(p => p.Uid).ToList()
                    }).ToListAsync(cancellationToken);*/
            }
            catch (Exception e)
            {
               throw new Exception($"Error getting categories: {e.Message}", e);
            }
        }
    }
}