using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using System.Linq;

namespace Core.Application.Mediatr.Stores.Commands;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateStoreCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        RuleFor(s => s)
            .MustAsync(StoreNameExists).WithMessage("Store with that name already exists.");
        
        RuleFor(s => s)
            .MustAsync(UniqueStoreNameExists).WithMessage("Store with that unique name already exists.");
        
        RuleFor(s => s)
            .MustAsync(SecondaryStoreEmailExists).WithMessage("Store with that secondary email already exists.");
    }

    private async Task<bool> StoreNameExists(UpdateStoreCommand command, CancellationToken ct)
    {
        var x = await _dbContext.Stores.Where(s => s.Uid != command.Uid).AllAsync(b => b.Name.Trim().ToLower() != command.Name.Trim().ToLower() && b.IsActive,
            ct);
        return x;
    }

    private async Task<bool> UniqueStoreNameExists(UpdateStoreCommand command, CancellationToken ct)
    {
        var uniqueNameNormalized = UsernameHelper.Normalize(command.UniqueName);

        var x = await _dbContext.Stores.Where(s => s.Uid != command.Uid).AllAsync(
            b => b.UniqueName.Trim().ToLower() != uniqueNameNormalized.Trim().ToLower() && b.IsActive, ct);
        return x;
    }

    private async Task<bool> SecondaryStoreEmailExists(UpdateStoreCommand command, CancellationToken ct)
    {
        var x = await _dbContext.Stores.Where(s => s.Uid != command.Uid).AllAsync(
            b => b.StoreEmail.Trim().ToLower() != command.StoreEmail.Trim().ToLower() && b.IsActive, ct);
        return x;
    }
}
