
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.Verify;

public class VerifyUserCommand : IRequest <Unit>
{
    public string? Id { get; set; }
}

public class VerifyUserCommandHandler : IRequestHandler<VerifyUserCommand,Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<VerifyUserCommandHandler> _logger;

    public VerifyUserCommandHandler(IApplicationDbContext dbContext, IMediator mediator, ILogger<VerifyUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(VerifyUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.Id && !u.IsSuspended,
                cancellationToken);

            if (user == null)
                throw new NotFoundException("User not found");

            user.IsVerified = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error verifying the user");
            throw;
        }
    }
}