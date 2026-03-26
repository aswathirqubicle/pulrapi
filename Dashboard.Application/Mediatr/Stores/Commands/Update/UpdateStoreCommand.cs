using AutoMapper;
using Core.Application.Mappings;
using Core.Application.Security.Validation.Attributes;
using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Dashboard.Application.Mediatr.Stores.Commands.Update;

public class UpdateStoreCommand : IRequest <Unit>
{
    [Required]
    public string? Uid { get; set; }

    [PulrNameValidation(true)]
    public string? Name { get; set; }
    [PulrNameValidation(true)]
    public string? LegalName { get; set; }
    [PulrNameValidation(true)]
    public string? UniqueName { get; set; }
    public string? StoreEmail { get; set; }
    public bool IsEmailPublic { get; set; }
    public string? CurrencyUid { get; set; }
    public string? Description { get; set; }
    public string? AffiliateId { get; set; }
}

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand,Unit>
{
    private readonly ILogger<UpdateStoreCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public UpdateStoreCommandHandler(ILogger<UpdateStoreCommandHandler> logger, IMapper mapper, IMediator mediator)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var requestMapped =  _mapper.Map<Core.Application.Mediatr.Stores.Commands.UpdateStoreCommand>(request);
            await _mediator.Send(requestMapped);
            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
