
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Dashboard.Application.Mediatr.Stores.Commands.Delete;

public class DeleteStoreCommand : IRequest <Unit>
{
    [Required]
    public string? StoreUid { get; set; }
}

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand,Unit>
{
    private readonly ILogger<DeleteStoreCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public DeleteStoreCommandHandler(ILogger<DeleteStoreCommandHandler> logger,IMapper mapper, IMediator mediator)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new Core.Application.Mediatr.Stores.Commands.DeleteStoreCommand() { Uid = request.StoreUid });
            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}