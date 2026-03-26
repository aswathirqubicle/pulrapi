

using Core.Application.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Mediatr.Users.Commands.Verify;

public class VerifyUserCommandValidator : AbstractValidator<VerifyUserCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public VerifyUserCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        RuleFor(c => c.Id)
            .NotEmpty().WithMessage("Please provide user Id")
            .MustAsync(UserExist!).WithMessage("User doesn't exist");
    }

    public async Task<bool> UserExist(VerifyUserCommand command, string id, CancellationToken ct)
    {
        var userExist = await _dbContext.Users.AnyAsync(u => u.Id == id && !u.IsSuspended, ct);

        return userExist;
    }
}