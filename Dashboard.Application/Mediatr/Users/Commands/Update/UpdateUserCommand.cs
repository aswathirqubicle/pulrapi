using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.Update;

public class UpdateUserCommand : IRequest <Unit>
{
    public string? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand,Unit>
{
    private readonly ILogger<UpdateUserCommandHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(ILogger<UpdateUserCommandHandler> logger, IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                throw new NotFoundException("User not found");

            if (user.UserName != request.UserName)
            {
                user.UsernameChangesCount += 1;
                user.UsernameChangeDate = DateTime.UtcNow;
            }

            _mapper.Map(request, user);

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