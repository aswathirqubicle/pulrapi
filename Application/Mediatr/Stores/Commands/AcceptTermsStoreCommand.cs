using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Commands
{
    public class AcceptTermsStoreCommand : IRequest<bool>
    {
        [Required]
        public string StoreUid { get; set; }
    }

    public class AcceptTermsStoreCommandHandler : IRequestHandler<AcceptTermsStoreCommand, bool>
    {
        private readonly ILogger<AcceptTermsStoreCommandHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _dbContext;

        public AcceptTermsStoreCommandHandler(ILogger<AcceptTermsStoreCommandHandler> logger, IMediator mediator, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _mediator = mediator;
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(AcceptTermsStoreCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var store = await _dbContext.Stores.Where(s => s.Uid == request.StoreUid).SingleOrDefaultAsync(cancellationToken);
                if (store == null)
                {
                    throw new NotFoundException();
                }

                store.TermsAccepted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }
    }
}
