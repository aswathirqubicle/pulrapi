using System.ComponentModel.DataAnnotations;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shortid;
using shortid.Configuration;

namespace Dashboard.Application.Mediatr.Products.Commands.Create;

public class CreateProductCommand : IRequest<string>
{
    [Required]
    public string? StoreUid { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public double Price { get; set; }

    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public List<string> Categories { get; set; } = new List<string>();
    public ICollection<ProductMoreInfoRequest> MoreInfos { get; set; } = new List<ProductMoreInfoRequest>();

    public ICollection<ProductVariantCreateRequest> ProductVariations { get; set; } =
        new List<ProductVariantCreateRequest>();

    public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
    public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, string>
{
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;

    public CreateProductCommandHandler(ILogger<CreateProductCommandHandler> logger, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public Task<string> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task<string> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        var store = await _dbContext.Stores.SingleOrDefaultAsync(s => s.IsActive && s.Uid == request.StoreUid,
    //            cancellationToken);

    //        if (store == null)
    //            throw new NotFoundException("Store not found");

    //        var product = new Product
    //        {
    //            ArticleCode = ShortId.Generate(new GenerationOptions(true, false)),
    //            Description = request.Description,
    //            Name = request.Name,
    //            Price = request.Price,
    //            Quantity = request.Quantity,
    //            Store = store,
    //            ProductAttributes = request.ProductVariations.Any()
    //                ? request.ProductVariations.Select(pv => new ProductAttribute()
    //                {
    //                    Key = pv.Key,
    //                    ProductAttributeValues = String.IsNullOrEmpty(pv.Values)
    //                        ? null
    //                        : pv.Values.Split('|').Select(v => new ProductAttributeValue() { Value = v }).ToList()
    //                }).ToList()
    //                : null
    //        };
    //        _dbContext.Products.Add(product);

    //        if (request.Categories.Any())
    //        {
    //            foreach (var categoryUid in request.Categories)
    //            {
    //                var category = await _dbContext.ProductCategories
    //                    .SingleOrDefaultAsync(c => c.IsActive && c.Uid == categoryUid, cancellationToken);

    //                if (category is null)
    //                    continue;

    //                product.ProductCategory.Add(new ProductCategory
    //                {
    //                    CategoryId = category.Id,
    //                    Product = product
    //                });
    //            }
    //        }

    //        if (request.MoreInfos.Any())
    //        {
    //            var newMoreInfos = new List<ProductMoreInfo>();
    //            foreach (var moreInfoRequest in request.MoreInfos)
    //            {
    //                newMoreInfos.Add(new ProductMoreInfo()
    //                {
    //                    Product = product,
    //                    Title = moreInfoRequest.Title,
    //                    Info = moreInfoRequest.Info
    //                });
    //            }

    //            if (newMoreInfos.Any())
    //            {
    //                _dbContext.ProductMoreInfos.AddRange(newMoreInfos);
    //            }
    //        }

    //        if (request.ProductPairArticleCodes.Any())
    //        {
    //            var newPairs = new List<ProductPair>();
    //            foreach (var pairArticleCode in request.ProductPairArticleCodes)
    //            {
    //                var pairByArticleCode =
    //                    await _dbContext.Products.SingleOrDefaultAsync(p => p.ArticleCode == pairArticleCode,
    //                        cancellationToken);
    //                if (pairByArticleCode != null)
    //                {
    //                    newPairs.Add(new ProductPair()
    //                    {
    //                        Product = product,
    //                        Pair = pairByArticleCode
    //                    });
    //                }
    //            }

    //            if (newPairs.Any())
    //            {
    //                _dbContext.ProductPairs.AddRange(newPairs);
    //            }
    //        }

    //        if (request.ProductSimilarArticleCodes.Any())
    //        {
    //            var newSimilars = new List<ProductSimilar>();
    //            foreach (var productSimilarArticleCode in request.ProductSimilarArticleCodes)
    //            {
    //                var productSimilar = await _dbContext.Products.SingleOrDefaultAsync(p =>
    //                    p.ArticleCode == productSimilarArticleCode && p.Store == store, cancellationToken);
    //                if (productSimilar != null)
    //                {
    //                    newSimilars.Add(new ProductSimilar
    //                    {
    //                        Product = product,
    //                        Similar = productSimilar
    //                    });
    //                }
    //            }

    //            if (newSimilars.Any())
    //            {
    //                _dbContext.ProductSimilars.AddRange(newSimilars);
    //            }
    //        }

    //        await _dbContext.SaveChangesAsync(cancellationToken);
    //        return product.Uid;
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogError(e, e.Message);
    //        throw;
    //    }
    //}
}