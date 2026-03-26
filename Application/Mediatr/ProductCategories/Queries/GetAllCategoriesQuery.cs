using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Categories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.ProductCategories.Queries
{
    public class GetAllMainCategoriesQuery : IRequest<List<CategoryResponse>>
    {
    }

    public class GetAllMainCategoriesQueryHandler : IRequestHandler<GetAllMainCategoriesQuery, List<CategoryResponse>>
    {
        private readonly ILogger<GetAllMainCategoriesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetAllMainCategoriesQueryHandler(ILogger<GetAllMainCategoriesQueryHandler> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<List<CategoryResponse>> Handle(GetAllMainCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.Categories.Select(c => new CategoryResponse()
                {
                    Slug = c.Slug,
                    Title = c.Name,
                    Uid = c.Uid
                }).ToListAsync(cancellationToken);
                //return await _storeService.GetMainCategories();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
