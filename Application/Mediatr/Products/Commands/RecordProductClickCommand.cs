using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Products.Commands
{
    public class RecordProductClickCommand : IRequest<ProductClickStatisticsResponse>
    {
        [Required]
        public string ProductUid { get; set; }
    }

    public class RecordProductClickCommandHandler : IRequestHandler<RecordProductClickCommand, ProductClickStatisticsResponse>
    {
        private readonly ILogger<RecordProductClickCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public RecordProductClickCommandHandler(
            ILogger<RecordProductClickCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<ProductClickStatisticsResponse> Handle(RecordProductClickCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                if (currentUser == null)
                {
                    throw new NotAuthenticatedException("User not authenticated");
                }

                var product = await _dbContext.Products
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Uid == request.ProductUid && p.IsActive, cancellationToken);

                if (product == null)
                {
                    throw new NotFoundException($"Product with uid {request.ProductUid} not found");
                }

                // Don't record clicks from the product owner, but still return statistics
                if (product.UserId != currentUser.Id)
                {
                    // Check if user already has a click record for this product
                    var existingClick = await _dbContext.ProductClicks
                        .FirstOrDefaultAsync(
                            pc => pc.ProductId == product.Id && pc.UserId == currentUser.Id,
                            cancellationToken);

                    if (existingClick != null)
                    {
                        existingClick.Count += 1;
                        existingClick.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var productClick = new ProductClick
                        {
                            ProductId = product.Id,
                            UserId = currentUser.Id,
                            Count = 1
                        };
                        _dbContext.ProductClicks.Add(productClick);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // Get all clicks for this product, excluding clicks from the product owner
                var productClicks = await _dbContext.ProductClicks
                    .Include(pc => pc.User)
                        .ThenInclude(u => u.Profile)
                    .Where(pc => pc.ProductId == product.Id && pc.UserId != product.UserId)
                    .ToListAsync(cancellationToken);

                // Group by user and calculate total clicks
                var userClickDetails = productClicks
                    .GroupBy(pc => pc.UserId)
                    .Select(g => new ProductClickUserDetail
                    {
                        UserUid = g.First().User.Profile?.Uid ?? g.First().User.Id,
                        UserName = g.First().User.UserName ?? g.First().User.DisplayName ?? g.First().User.FirstName?.Trim(),
                        ClickCount = g.Sum(pc => pc.Count)
                    })
                    .OrderByDescending(u => u.ClickCount)
                    .ToList();

                var totalClicks = productClicks.Sum(pc => pc.Count);

                return new ProductClickStatisticsResponse
                {
                    ProductUid = product.Uid,
                    ProductName = product.Name,
                    TotalClicks = totalClicks,
                    ClickedUsers = userClickDetails
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}

