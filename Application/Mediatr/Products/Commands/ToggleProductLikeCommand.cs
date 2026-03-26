using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Mediatr.Products.Notifications;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ToggleProductLikeCommand : IRequest<bool>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class ToggleProductLikeCommandHandler : IRequestHandler<ToggleProductLikeCommand, bool>
    {
        private readonly ILogger<ToggleProductLikeCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly IStoreService _storeService;
        private readonly INotificationService _notificationService;

        public ToggleProductLikeCommandHandler(
            ILogger<ToggleProductLikeCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext,
            IMediator mediator,
            IStoreService storeService,
            INotificationService notificationService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _mediator = mediator;
            _storeService = storeService;
            _notificationService = notificationService;
        }
        public async Task<bool> Handle(ToggleProductLikeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Uid == request.Uid, cancellationToken);

                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"Profile doesnt exist for user '{cUser.Id}' .");
                }

                if (product == null)
                {
                    throw new BadRequestException($"Product with uid {request.Uid} doesnt exist.");
                }

                var existingProductLike = await _dbContext.ProductLikes
                    .Include(pl => pl.Product)
                    .SingleOrDefaultAsync(l => l.Product.Uid == request.Uid && l.LikedBy.Uid == cUser.Profile.Uid, cancellationToken);

                var likedByMe = false;
                if (existingProductLike == null)
                {
                    _dbContext.ProductLikes.Add(new ProductLike() { Product = product, LikedBy = cUser.Profile });
                    await _notificationService.SaveLikeNotificationAsync(cUser.Id, request.Uid, EntityTypeEnum.PRODUCT, ActivityActionTypeEnum.LikeProduct);
                    likedByMe = true;
                }
                else
                {
                    _dbContext.ProductLikes.Remove(existingProductLike);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                await _mediator.Publish(new ProductLikeToggledNotification() { ProductUid = product.Uid }, cancellationToken);

                return likedByMe;
                
                
                //return await _storeService.ProductToggleLike(request.Uid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
