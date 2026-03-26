using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Notifications;
using Core.Application.Mediatr.Stores.NotificationHandlers;

namespace Core.Application.Mediatr.Stores.NotificationHandlers
{
    public class RecalculateStoreLikesNotificationHandler : INotificationHandler<ProductLikeToggledNotification>
    {
        private readonly ILogger<RecalculateStoreLikesNotificationHandler> _logger;
        private readonly IApplicationDbContext _dbContext;

        public RecalculateStoreLikesNotificationHandler(
            ILogger<RecalculateStoreLikesNotificationHandler> logger,
            IApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(ProductLikeToggledNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _dbContext.Products.Include(p => p.Store)
                    .SingleOrDefaultAsync(p => p.Uid == notification.ProductUid, cancellationToken);
                if (product == null)
                {
                    throw new Exception(
                        $"RecalculateStoreLikes, product with uid '{notification.ProductUid}' doesnt exist.");
                }

                product.Store.LikesCount = await _dbContext.ProductLikes.Where(pl => pl.Product.Store == product.Store)
                    .CountAsync(cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                //await _storeService.RecalculateStoreLikes(notification.ProductUid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
