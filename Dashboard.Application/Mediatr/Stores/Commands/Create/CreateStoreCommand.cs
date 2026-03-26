using System.ComponentModel.DataAnnotations;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.Commands.Create;

public class CreateStoreCommand : IRequest<string>
{
    [Required] public string? UserId { get; set; }
    [Required] [PulrNameValidation] public string? Name { get; set; }

    [Required] [PulrNameValidation] public string? LegalName { get; set; }

    [Required] [PulrNameValidation] public string? UniqueName { get; set; }
    [Required] public string? StoreEmail { get; set; }
    [Required] public string? CurrencyUid { get; set; }
}

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, string>
{
    private readonly ILogger<CreateStoreCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _dbContext;

    public CreateStoreCommandHandler(ILogger<CreateStoreCommandHandler> logger, IMediator mediator,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _mediator = mediator;
        _dbContext = dbContext;
    }

    public async Task<string> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var uniqueName = UsernameHelper.Normalize(request.UniqueName);

            var user = await _dbContext.Users.SingleOrDefaultAsync(u => !u.IsSuspended && u.Id == request.UserId,
                cancellationToken);

            if (user == null)
            {
                throw new BadRequestException("User doesn't exist.");
            }

            var store = new Store
            {
                Name = request.Name,
                LegalName = request.LegalName,
                UniqueName = uniqueName,
                StoreEmail = request.StoreEmail,
                Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == request.CurrencyUid,
                    cancellationToken)
            };

            store.User = user;
            
            store.AddDomainEvent(new StoreCreatedEvent(store));

            _dbContext.Stores.Add(store);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return store.Uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating store");
            throw;
        }
    }
}