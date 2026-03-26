using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.BagItems;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.BagItems.Commands
{
    public class UpdateBagItemsCommand : IRequest <Unit>
    {
        public List<BagProductDto> Products { get; set; }
    }

    public class UpdateBagItemsCommandHandler : IRequestHandler<UpdateBagItemsCommand,Unit>
    {
        private readonly ILogger<UpdateBagItemsCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public UpdateBagItemsCommandHandler(ILogger<UpdateBagItemsCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(UpdateBagItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(true);

                var existingBagProducts = await _dbContext.UserBagProducts.Where(bp => bp.User.Id == cUser.Id).ToListAsync();
                if (existingBagProducts.Count > 0)
                {
                    _dbContext.UserBagProducts.RemoveRange(existingBagProducts);
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                }

                var bagProducts = request.Products;
                if (bagProducts == null || bagProducts.Count == 0)
                {
                    return Unit.Value;
                }

                foreach (var bProduct in bagProducts)
                {
                    var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Uid == bProduct.Uid);
                    if (product != null)
                    {
                        var userBagProduct = new UserBagProduct()
                        {
                            BagProduct = product,
                            Quantity = bProduct.BagQuantity,
                            User = cUser,
                            ProductVariantCombinationUid = bProduct.ProductVariantCombinationUid
                        };
                        _dbContext.UserBagProducts.Add(userBagProduct);
                    }
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
