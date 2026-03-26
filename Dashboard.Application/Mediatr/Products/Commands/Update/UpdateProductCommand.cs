using System.ComponentModel.DataAnnotations;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Products.Commands.Update;

public class UpdateProductCommand : IRequest <Unit>
{
    [Required]
    public string? Uid { get; set; }

    public string? Name { get; set; }
    public double Price { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public List<string> Categories { get; set; } = new List<string>();
    public string? ArticleCode { get; set; }
    public ICollection<ProductMoreInfoUpdateRequest> MoreInfos { get; set; } = new List<ProductMoreInfoUpdateRequest>();
    public ICollection<ProductAttributeUpdateRequest> ProductVariations { get; set; } = new List<ProductAttributeUpdateRequest>();
    public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
    public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand,Unit>
{
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;

    public UpdateProductCommandHandler(ILogger<UpdateProductCommandHandler> logger, IApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        var product = _dbContext.Products.Include(p => p.Store).SingleOrDefault(p => p.IsActive && p.Uid == request.Uid);

    //        if (product == null)
    //            throw new NotFoundException("Product not found");

    //        product.Quantity = request.Quantity;

    //        if (!String.IsNullOrWhiteSpace(request.Name))
    //        {
    //            product.Name = request.Name;
    //        }

    //        if (!String.IsNullOrWhiteSpace(request.Description))
    //        {
    //            product.Description = request.Description;
    //        }

    //        if (request.Categories.Any())
    //        {
    //            foreach (var categoryUid in request.Categories)
    //            {
    //                var category = await _dbContext.ProductCategories
    //                    .SingleOrDefaultAsync(c => c.Uid == categoryUid, cancellationToken);

    //                if (category is null)
    //                    continue;

    //                product.ProductCategory.Add(new ProductCategory
    //                {
    //                    CategoryId = category.Id,
    //                    Product = product
    //                });
    //            }
    //        }

    //        if (String.IsNullOrWhiteSpace(request.ArticleCode) && product.ArticleCode != request.ArticleCode)
    //        {
    //            var articleCodeExists = await _dbContext.Products
    //                .AnyAsync(p => p.Store == product.Store &&
    //                               p.ArticleCode == request.ArticleCode &&
    //                               p.Uid != product.Uid, cancellationToken);

    //            product.ArticleCode = request?.ArticleCode?.Trim();
    //        }

    //        if(request == null)
    //        {
    //            throw new ArgumentNullException(nameof(request), "Request cannot be null");
    //        }
    //        var productMoreInfosToRemove = await _dbContext.ProductMoreInfos.Where(v => v.Product == product &&
    //                                                                                    request.MoreInfos.Select(pv => pv.Uid).Contains(v.Uid) == false)
    //            .ToListAsync(cancellationToken);

    //        if (productMoreInfosToRemove.Any())
    //        {
    //            _dbContext.ProductMoreInfos.RemoveRange(productMoreInfosToRemove);
    //        }

    //        foreach (var moreInfoRequest in request.MoreInfos)
    //        {
    //            var moreInfo = await _dbContext.ProductMoreInfos
    //                .Where(e => e.Product == product && e.Uid == moreInfoRequest.Uid)
    //                .SingleOrDefaultAsync(cancellationToken);

    //            if (moreInfo != null)
    //            {
    //                if (moreInfo.Title != moreInfoRequest.Title)
    //                {
    //                    moreInfo.Title = moreInfoRequest.Title;
    //                }

    //                if (moreInfo.Info != moreInfoRequest.Info)
    //                {
    //                    moreInfo.Info = moreInfoRequest.Info;
    //                }
    //            }

    //            if (String.IsNullOrWhiteSpace(moreInfoRequest.Uid))
    //            {
    //                moreInfo = new ProductMoreInfo
    //                {
    //                    Product = product,
    //                    Title = moreInfoRequest.Title,
    //                    Info = moreInfoRequest.Info
    //                };
    //                _dbContext.ProductMoreInfos.Add(moreInfo);
    //            }
    //        }

    //        var productVariationsToRemove = await _dbContext.ProductAttributes
    //            .Where(v => v.Product == product &&
    //                        request.ProductVariations
    //                            .Select(pv => pv.Uid)
    //                            .Contains(v.Uid) == false)
    //            .ToListAsync(cancellationToken);

    //        if (productVariationsToRemove.Any())
    //        {
    //            _dbContext.ProductAttributes.RemoveRange(productVariationsToRemove);
    //        }

    //        foreach (var attributeRequest in request.ProductVariations)
    //        {
    //            var productAttribute = await _dbContext.ProductAttributes
    //                .Where(pv => pv.Product == product && pv.Uid == attributeRequest.Uid)
    //                .SingleOrDefaultAsync(cancellationToken);

    //            if (productAttribute != null)
    //            {
    //                if (productAttribute.Key != attributeRequest.Key)
    //                {
    //                    productAttribute.Key = attributeRequest.Key;
    //                }
    //            }
    //            else if (String.IsNullOrWhiteSpace(attributeRequest.Uid))
    //            {
    //                productAttribute = new ProductAttribute
    //                {
    //                    Product = product,
    //                    Key = attributeRequest.Key,
    //                };

    //                _dbContext.ProductAttributes.Add(productAttribute);
    //            }
    //        }

    //        request.ProductSimilarArticleCodes = request.ProductSimilarArticleCodes
    //            .Select(p => p.Trim()).ToList();

    //        var productSimilarsToRemove = await _dbContext.ProductSimilars
    //            .Where(ps => ps.Product == product &&
    //                         request.ProductSimilarArticleCodes.Contains(ps.Similar.ArticleCode) == false)
    //            .ToListAsync(cancellationToken);

    //        if (productSimilarsToRemove.Any())
    //        {
    //            _dbContext.ProductSimilars.RemoveRange(productSimilarsToRemove);
    //        }

    //        foreach (var productSimilarArticleCode in request.ProductSimilarArticleCodes)
    //        {
    //            var productSimilarExisting = await _dbContext.ProductSimilars
    //                .SingleOrDefaultAsync(ps =>
    //                    ps.Similar.ArticleCode == productSimilarArticleCode &&
    //                    ps.Product == product &&
    //                    ps.Product.Store == product.Store, cancellationToken);

    //            if (productSimilarExisting == null)
    //            {
    //                var productLikeThis = await _dbContext.Products.SingleOrDefaultAsync(p =>
    //                        p.ArticleCode == productSimilarArticleCode && p.Store.Id == product.Store.Id,
    //                    cancellationToken);

    //                if (productLikeThis != null)
    //                {
    //                    _dbContext.ProductSimilars.Add(new ProductSimilar
    //                        { Product = product, Similar = productLikeThis });
    //                }
    //            }
    //        }

    //        request.ProductPairArticleCodes = request.ProductPairArticleCodes.Select(p => p.Trim()).ToList();

    //        var productPairsToRemove = await _dbContext.ProductPairs.Where(ps => ps.Product == product &&
    //                                                                             request.ProductPairArticleCodes.Contains(ps.Pair.ArticleCode) == false)
    //            .ToListAsync(cancellationToken);

    //        if (productPairsToRemove.Any())
    //        {
    //            _dbContext.ProductPairs.RemoveRange(productPairsToRemove);
    //        }

    //        foreach (var productPairArticleCode in request.ProductPairArticleCodes)
    //        {
    //            var productPairExisting = await _dbContext.ProductPairs.SingleOrDefaultAsync(ps =>
    //                ps.Pair.ArticleCode == productPairArticleCode &&
    //                ps.Product == product &&
    //                ps.Product.Store == product.Store, cancellationToken);
    //            if (productPairExisting == null)
    //            {
    //                var productLikeThis = await _dbContext.Products.SingleOrDefaultAsync(p =>
    //                    p.ArticleCode == productPairArticleCode && p.Store == product.Store, cancellationToken);

    //                if (productLikeThis != null)
    //                {
    //                    _dbContext.ProductPairs.Add(new ProductPair()
    //                        { Product = product, Pair = productLikeThis });
    //                }
    //            }
    //        }

    //        await _dbContext.SaveChangesAsync(cancellationToken);
    //        return Unit.Value;
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogError(e, e.Message);
    //        throw;
    //    }
    //}
}