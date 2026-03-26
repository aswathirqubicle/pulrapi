using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.ProductCategories.Commands
{
    public class DeleteProductCategoryCommand : IRequest <Unit>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class DeleteProductCategoryCommandHandler : IRequestHandler<DeleteProductCategoryCommand,Unit>
    {
        private readonly ILogger<DeleteProductCategoryCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public DeleteProductCategoryCommandHandler(ILogger<DeleteProductCategoryCommandHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public Task<Unit> Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //todo rewrite this to match new listing
                throw new NotImplementedException();
                /*
                var cToDelete = await _dbContext.ProductCategories.Include(pc => pc.Products)
                    .SingleOrDefaultAsync(c => c.Uid == request.Uid && c.Store.User.Id == _currentUserService.GetUserId(),
                        cancellationToken);
                if (cToDelete == null)
                {
                    throw new BadRequestException("Store doesn't exist.");
                }

                _dbContext.ProductCategories.Remove(cToDelete);
                await _dbContext.SaveChangesAsync(cancellationToken);
                */

                // await _storeService.DeleteCategory(request.Uid);
                //return Unit.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}