using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Wishlist.Commands
{
    public class RemoveFromWishlistCommand : IRequest<Unit>
    {
        [Required]
        public string ProductUid { get; set; }
        
        public string ProductVariantCombinationUid { get; set; }
    }

    public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Unit>
    {
        private readonly ILogger<RemoveFromWishlistCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public RemoveFromWishlistCommandHandler(
            ILogger<RemoveFromWishlistCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
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

                // Find wishlist item
                var wishlistItem = await _dbContext.UserWishlistProducts
                    .FirstOrDefaultAsync(w => w.UserId == user.Id 
                        && w.WishlistProductId == product.Id
                        && (string.IsNullOrEmpty(request.ProductVariantCombinationUid) 
                            ? string.IsNullOrEmpty(w.ProductVariantCombinationUid)
                            : w.ProductVariantCombinationUid == request.ProductVariantCombinationUid),
                        cancellationToken);

                if (wishlistItem == null)
                {
                    throw new Core.Application.Exceptions.NotFoundException("Product not found in wishlist");
                }

                _dbContext.UserWishlistProducts.Remove(wishlistItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error removing product from wishlist: {Message}", e.Message);
                throw;
            }
        }
    }
}

