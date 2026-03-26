using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.BagItems.Commands
{
    public class UpdateBagItemQuantityCommand : IRequest<Unit>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class UpdateBagItemQuantityCommandHandler : IRequestHandler<UpdateBagItemQuantityCommand, Unit>
    {
        private readonly ILogger<UpdateBagItemQuantityCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public UpdateBagItemQuantityCommandHandler(
            ILogger<UpdateBagItemQuantityCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(UpdateBagItemQuantityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(true);
                if (user == null)
                {
                    throw new Core.Application.Exceptions.NotAuthenticatedException("User not authenticated");
                }

                // Find product
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Uid == request.ProductUid, cancellationToken);

                if (product == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found");
                }

                // Find bag item
                var bagItem = await _dbContext.UserBagProducts
                    .Include(b => b.ProductVariantCombination)
                    .FirstOrDefaultAsync(b => b.UserId == user.Id 
                        && b.BagProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(b.ProductVariantCombinationUid)
                            : b.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                if (bagItem == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found in bag");
                }

                // Validate quantity against variant stock if variant combination exists
                if (bagItem.ProductVariantCombination != null)
                {
                    if (!bagItem.ProductVariantCombination.IsAvailable)
                    {
                        throw new Core.Application.Exceptions.BadRequestException("Variant is not available");
                    }

                    var availableStock = bagItem.ProductVariantCombination.Quantity;
                    if (availableStock < request.Quantity)
                    {
                        var message = $"Requested quantity exceeds available stock. Available quantity: {availableStock}.";
                        throw new Core.Application.Exceptions.BadRequestException(message);
                    }
                }

                bagItem.Quantity = request.Quantity;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error updating bag item quantity: {Message}", e.Message);
                throw;
            }
        }
    }
}

