using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Delete;
using Core.Application.Mediatr.Users.Commands.Register;
using Microsoft.EntityFrameworkCore;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Users.Commands.Delete
{
    public class DeleteUserCommand : IRequest <Unit> { }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand,Unit>
    {
        private readonly ILogger<RegisterCommandHandler> logger;
        private readonly ICurrentUserService currentUserService;
        private readonly IUserService userService;
        private readonly IApplicationDbContext dbContext;

        public DeleteUserCommandHandler(ILogger<RegisterCommandHandler> logger,
            ICurrentUserService currentUserService,
            IUserService userService,
            IApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.currentUserService = currentUserService;
            this.userService = userService;
            this.dbContext = dbContext;
        }

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await currentUserService.GetUserAsync(true);
                if (currentUserService.HasRole(PulrRoles.Administrator))
                {
                    throw new ForbiddenException("Delete yourself? Are u drunk?");
                }

                // Check active buyer orders
                var hasActiveBuyOrders = await dbContext.Orders
                    .AnyAsync(o => o.Profile.UserId == currentUser.Id && 
                                  (o.OrderStatus == OrderStatusEnum.Pending || 
                                   o.OrderStatus == OrderStatusEnum.Processing ||
                                   o.OrderStatus == OrderStatusEnum.Shipped ||
                                   o.OrderStatus == OrderStatusEnum.Delivered), 
                              cancellationToken);
                
                if (hasActiveBuyOrders)
                     throw new BadRequestException("You cannot delete your account because you have active orders in progress.");

                // Check active seller orders
                var hasActiveSellOrders = await dbContext.OrderProductAffiliates
                    .AnyAsync(opa => opa.Product.UserId == currentUser.Id && 
                                    (opa.OrderItemStatus == OrderStatusEnum.Pending || 
                                     opa.OrderItemStatus == OrderStatusEnum.Processing ||
                                     opa.OrderItemStatus == OrderStatusEnum.Shipped ||
                                     opa.OrderItemStatus == OrderStatusEnum.Delivered), 
                              cancellationToken);
                
                if (hasActiveSellOrders)
                     throw new BadRequestException("You cannot delete your account because you have active orders to fulfill.");

                await userService.DeleteAccountAsync(currentUser);

                return Unit.Value;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
