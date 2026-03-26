using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;

namespace Core.Application.Mediatr.Stores.Commands;

public class AcceptTermsStoreCommandValidator : AbstractValidator<AcceptTermsStoreCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AcceptTermsStoreCommandValidator(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        RuleFor(s => s.StoreUid)
            .MustAsync(StoreExists).WithMessage("Store doesn't exist.");
    }

    private async Task<bool> StoreExists(string storeUid, CancellationToken ct)
    {
        var x = await _dbContext.Stores.AnyAsync(e => e.Uid == storeUid && e.IsActive && e.UserId == _currentUserService.GetUserId() , ct);
        return x;
    }
   
}
