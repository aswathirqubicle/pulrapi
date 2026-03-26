using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Fulfillments.Commands;

namespace Core.Application.Mediatr.Fulfillments.Commands
{
    public class DeleteFulfillmentCommand : IRequest <Unit>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class DeleteFulfillmentCommandHandler : IRequestHandler<DeleteFulfillmentCommand,Unit>
    {
        private readonly ILogger<DeleteFulfillmentCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public DeleteFulfillmentCommandHandler(ILogger<DeleteFulfillmentCommandHandler> logger, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<Unit> Handle(DeleteFulfillmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var fulfillment = await _dbContext.Fulfillments
                                            .SingleOrDefaultAsync(f => f.Uid == request.Uid &&
                                                                cUser.Stores.Select(s => s.Id).Contains(f.Store.Id));

                if (fulfillment == null)
                {
                    throw new BadRequestException("Fulfillment doesn't exist");
                }

                fulfillment.IsActive = false;
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
