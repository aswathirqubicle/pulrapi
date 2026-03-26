using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.ToggleSuspendUser;

public class ToggleSuspendUserCommand : IRequest <Unit>
{
    [Required]
    public string? UserUid { get; set; }
    public bool IsSuspended { get; set; } = true;
    public int UnsuspendInDays { get; set; } = 0;
}

public class ToggleSuspendUserCommandHandler : IRequestHandler<ToggleSuspendUserCommand,Unit>
{
    private readonly ILogger<ToggleSuspendUserCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public ToggleSuspendUserCommandHandler(ILogger<ToggleSuspendUserCommandHandler> logger, IApplicationDbContext dbContext, IMapper mapper)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(ToggleSuspendUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(e => e.Id == request.UserUid, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            var dateTimeNow = DateTime.UtcNow;
            user.IsSuspended = request.IsSuspended;

            if (user.IsSuspended) { user.SuspendedAt = dateTimeNow; }
            else { user.SuspendedAt = null; }

            if (user.IsSuspended && request.UnsuspendInDays > 0)
            {
                user.SuspendedUntil = dateTimeNow.AddDays(request.UnsuspendInDays);
            }
            else { user.SuspendedUntil = null; }

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}