using Core.Application.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Mediatr.Products.Commands.Update;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IApplicationDbContext _dbContext;

    //public UpdateProductCommandValidator(IApplicationDbContext dbContext)
    //{
    //    _dbContext = dbContext;
    //    RuleFor(p => p.ArticleCode)
    //        .MustAsync(UniqueArticleCode).WithMessage("Article code already exists");
    //}


    //public async Task<bool> UniqueArticleCode(UpdateProductCommand command, string? articleCode, CancellationToken ct)
    //{
    //    var product = _dbContext.Products.SingleOrDefault(p => p.IsActive && p.Uid == command.Uid);

    //    //if(string.IsNullOrEmpty(articleCode)) { return true; }

    //    if (product == null) { return false; }
    //    var x = await _dbContext.Products.AnyAsync(p => p.Store == product.Store &&
    //                                                    p.ArticleCode == command.ArticleCode &&
    //                                                    p.Uid != product.Uid, ct);

    //    return !x;
    //}
}