using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Models;
using System;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductDeleteCommand : IRequest<Unit>
    {
        [Required] public string Uid { get; set; }
    }

    public class ProductDeleteCommandHandler : IRequestHandler<ProductDeleteCommand,Unit>
    {
        private readonly ILogger<ProductDeleteCommandHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ProductDeleteCommandHandler(
            ILogger<ProductDeleteCommandHandler> logger,
            IConfiguration configuration,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _configuration = configuration;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(ProductDeleteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(includeStores: true)
                    ?? throw new UnauthorizedAccessException("User not found or not logged in");

                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Uid == request.Uid && p.UserId == user.Id, cancellationToken);

                if (product == null || !product.IsActive)
                    throw new BadRequestException("Product doesn't exist or is already deleted.");

                // Check for active orders associated with this product
                var hasActiveOrders = await _dbContext.OrderProductAffiliates
                    .AnyAsync(opa => opa.ProductId == product.Id && 
                                   (opa.OrderItemStatus == OrderStatusEnum.Pending || 
                                    opa.OrderItemStatus == OrderStatusEnum.Processing ||
                                    opa.OrderItemStatus == OrderStatusEnum.Shipped ||
                                    opa.OrderItemStatus == OrderStatusEnum.Delivered), 
                        cancellationToken);

                if (hasActiveOrders)
                    throw new BadRequestException("This product cannot be deleted because it has active orders that are still being processed.");

                // Mark the product as inactive
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                return Unit.Value;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with UID {Uid}", request.Uid);
                throw new ApplicationException("An error occurred while deleting the product.");
            }
        }
    }
}
