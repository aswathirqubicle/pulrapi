using FluentValidation;
using Core.Application.Interfaces;

namespace Dashboard.Application.Mediatr.Products.Commands.Create;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateProductCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        // TODO
        //RuleFor(e => e.Uid)
        //    .NotEmpty().WithMessage("Uid can't be empty.");
    }
        
}