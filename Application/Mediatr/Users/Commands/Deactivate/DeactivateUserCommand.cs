using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Users.Commands.Deactivate
{
    public class DeactivateUserCommand : IRequest<Unit>
    {
        public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Unit>
        {
            private readonly ILogger<DeactivateUserCommandHandler> _logger;
            private readonly ICurrentUserService _currentUserService;
            private readonly UserManager<User> _userManager;
            private readonly IUserService _userService;
            private readonly IApplicationDbContext _dbContext;

            public DeactivateUserCommandHandler(
                ILogger<DeactivateUserCommandHandler> logger,
                ICurrentUserService currentUserService,
                UserManager<User> userManager,
                IUserService userService,
                IApplicationDbContext dbContext)
            {
                _logger = logger;
                _currentUserService = currentUserService;
                _userManager = userManager;
                _userService = userService;
                _dbContext = dbContext;
            }

            public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
            {
                var currentUser = await _userManager.FindByIdAsync(_currentUserService.GetUserId());
                if (currentUser == null)
                {
                    throw new NotFoundException("User not found.");
                }

                // Check active buyer orders
                var hasActiveBuyOrders = await _dbContext.Orders
                    .AnyAsync(o => o.Profile.UserId == currentUser.Id && 
                                  (o.OrderStatus == OrderStatusEnum.Pending || 
                                   o.OrderStatus == OrderStatusEnum.Processing ||
                                   o.OrderStatus == OrderStatusEnum.Shipped ||
                                   o.OrderStatus == OrderStatusEnum.Delivered), 
                              cancellationToken);
                
                if (hasActiveBuyOrders)
                     throw new BadRequestException("You cannot deactivate your account because you have active orders in progress.");

                // Check active seller orders
                var hasActiveSellOrders = await _dbContext.OrderProductAffiliates
                    .AnyAsync(opa => opa.Product.UserId == currentUser.Id && 
                                    (opa.OrderItemStatus == OrderStatusEnum.Pending || 
                                     opa.OrderItemStatus == OrderStatusEnum.Processing ||
                                     opa.OrderItemStatus == OrderStatusEnum.Shipped ||
                                     opa.OrderItemStatus == OrderStatusEnum.Delivered), 
                              cancellationToken);
                
                if (hasActiveSellOrders)
                     throw new BadRequestException("You cannot deactivate your account because you have active orders to fulfill.");

                await _userService.DeactivateAccountAsync(currentUser);
                return Unit.Value;
            }
        }
    }
} 