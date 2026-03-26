using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetProductOwnerStatisticsQuery : IRequest<ProductOwnerStatisticsListResponse>
    {
    }

    public class GetProductOwnerStatisticsQueryHandler : IRequestHandler<GetProductOwnerStatisticsQuery, ProductOwnerStatisticsListResponse>
    {
        private readonly ILogger<GetProductOwnerStatisticsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public GetProductOwnerStatisticsQueryHandler(
            ILogger<GetProductOwnerStatisticsQueryHandler> logger,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<ProductOwnerStatisticsListResponse> Handle(GetProductOwnerStatisticsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Get all distinct user IDs who have active products
                var userIdsWithProducts = await _dbContext.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (!userIdsWithProducts.Any())
                {
                    return new ProductOwnerStatisticsListResponse();
                }

                // Get all users who have products
                var users = await _dbContext.Users
                    .Include(u => u.Profile)
                    .Where(u => userIdsWithProducts.Contains(u.Id))
                    .ToListAsync(cancellationToken);

                // Get all active products grouped by user
                var allProducts = await _dbContext.Products
                    .Where(p => p.IsActive && userIdsWithProducts.Contains(p.UserId))
                    .ToListAsync(cancellationToken);

                // Get all product clicks, excluding clicks from product owners
                var allProductClicks = await _dbContext.ProductClicks
                    .Where(pc => allProducts.Select(p => p.Id).Contains(pc.ProductId))
                    .ToListAsync(cancellationToken);

                var ownerStatisticsList = new List<ProductOwnerStatisticsResponse>();

                // Calculate statistics for each user
                foreach (var user in users)
                {
                    var userProducts = allProducts.Where(p => p.UserId == user.Id).ToList();
                    var userProductIds = userProducts.Select(p => p.Id).ToList();

                    // Get clicks for this user's products, excluding clicks from the product owner
                    var userProductClicks = allProductClicks
                        .Where(pc => userProductIds.Contains(pc.ProductId) && pc.UserId != user.Id)
                        .ToList();

                    // Calculate statistics per product
                    var productSummaries = userProducts.Select(product =>
                    {
                        var clicksForProduct = userProductClicks
                            .Where(pc => pc.ProductId == product.Id)
                            .Sum(pc => pc.Count);

                        return new ProductClickSummary
                        {
                            ProductUid = product.Uid,
                            ProductName = product.Name,
                            ClickCount = clicksForProduct
                        };
                    }).ToList();

                    // Calculate totals
                    var totalProducts = userProducts.Count;
                    var totalClicks = userProductClicks.Sum(pc => pc.Count);
                    var averageClicks = totalProducts > 0 ? (double)totalClicks / totalProducts : 0;

                    ownerStatisticsList.Add(new ProductOwnerStatisticsResponse
                    {
                        OwnerUserId = user.Id,
                        OwnerUsername = user.UserName ?? string.Empty,
                        OwnerDisplayName = user.DisplayName ?? user.FirstName?.Trim() ?? string.Empty,
                        OwnerEmail = user.Email ?? string.Empty,
                        TotalProducts = totalProducts,
                        TotalClicks = totalClicks,
                        AverageClicks = Math.Round(averageClicks, 2),
                        Products = productSummaries
                    });
                }

                return new ProductOwnerStatisticsListResponse
                {
                    Owners = ownerStatisticsList.OrderByDescending(o => o.TotalClicks).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting product owner statistics");
                throw;
            }
        }
    }
}

