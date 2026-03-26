using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Commands;

namespace Core.Application.Mediatr.Stores.Commands;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    private readonly IApplicationDbContext _dbContext;
    

    public CreateStoreCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        RuleFor(s => s.Name)
            .MustAsync(StoreNameExists).WithMessage("Store with that name already exists.");
        
        RuleFor(s => s.UniqueName)
            .MustAsync(UniqueStoreNameExists).WithMessage("Store with that unique name already exists.");
        
        RuleFor(s => s.StoreEmail)
            .MustAsync(SecondaryStoreEmailExists).WithMessage("Store with that secondary email already exists.");
    }

    private async Task<bool> StoreNameExists(string name, CancellationToken ct)
    {
        var x = await _dbContext.Stores.AllAsync(b => b.Name.Trim().ToLower() != name.Trim().ToLower() && b.IsActive, ct);
        return x;
    }
    
    private async Task<bool> UniqueStoreNameExists(string uniqueName, CancellationToken ct)
    {
        var uniqueNameNormalized = UsernameHelper.Normalize(uniqueName);
        
        var x = await _dbContext.Stores.AllAsync(b => b.UniqueName.Trim().ToLower() != uniqueNameNormalized.Trim().ToLower() && b.IsActive, ct);
        return x;
    }
    
    private async Task<bool> SecondaryStoreEmailExists(string email, CancellationToken ct)
    {
        var x = await _dbContext.Stores.AllAsync(b => b.StoreEmail.Trim().ToLower() != email.Trim().ToLower() && b.IsActive, ct);
        return x;
    }
}
