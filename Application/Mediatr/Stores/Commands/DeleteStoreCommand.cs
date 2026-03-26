using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Constants;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class DeleteStoreCommand : IRequest <Unit>
    {
        [Required] public string Uid { get; set; }
    }

    public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand,Unit>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public DeleteStoreCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
        {
            var isAdmin = _currentUserService.HasRole(PulrRoles.Administrator);

            var storeToDelete = await _dbContext.Stores
                .Include(s => s.StoreFollowers)
                .SingleOrDefaultAsync(s => s.Uid == request.Uid && (isAdmin || s.User.Id == _currentUserService.GetUserId()),
                    cancellationToken);

            if (storeToDelete == null)
                throw new NotFoundException("Store wasn't found.");


            storeToDelete.IsActive = false;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            return Unit.Value;
        }
    }
}
