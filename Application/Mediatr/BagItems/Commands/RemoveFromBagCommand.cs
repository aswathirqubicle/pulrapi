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
    public class RemoveFromBagCommand : IRequest<Unit>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
    }

    public class RemoveFromBagCommandHandler : IRequestHandler<RemoveFromBagCommand, Unit>
    {
        private readonly ILogger<RemoveFromBagCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public RemoveFromBagCommandHandler(
            ILogger<RemoveFromBagCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(RemoveFromBagCommand request, CancellationToken cancellationToken)
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

                _dbContext.UserBagProducts.Remove(bagItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error removing product from bag: {Message}", e.Message);
                throw;
            }
        }
    }
}

