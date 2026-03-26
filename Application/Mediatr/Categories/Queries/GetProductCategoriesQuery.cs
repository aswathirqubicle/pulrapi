using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Categories.Queries;

public class GetProductCategoriesQuery : IRequest<List<ProductCategoryResponse>>
{
}

public class GetProductCategoriesQueryHandler : IRequestHandler<GetProductCategoriesQuery, List<ProductCategoryResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetProductCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductCategoryResponse>> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.SubCategoryLevel1s
            .Where(s => s.Slug.StartsWith("product-"))
            .OrderBy(s => s.Id)
            .Select(s => new ProductCategoryResponse
            {
                Id = s.Id,
                Uid = s.Uid,
                Name = s.Name,
                Slug = s.Slug,
                SubCategories = s.SubCategoriesLevel2s
                    .OrderBy(sc => sc.Id)
                    .Select(sc => new ProductSubCategoryResponse
                    {
                        Id = sc.Id,
                        Uid = sc.Uid,
                        Name = sc.Name,
                        Slug = sc.Slug
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        return categories;
    }
}

public class ProductCategoryResponse
{
    public int Id { get; set; }
    public string Uid { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public List<ProductSubCategoryResponse> SubCategories { get; set; }
}

public class ProductSubCategoryResponse
{
    public int Id { get; set; }
    public string Uid { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
}
