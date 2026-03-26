using FluentValidation;

namespace Core.Application.Mediatr.Payments.Commands.Create;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.");

        RuleFor(x => x.Products)
            .NotNull()
            .WithMessage("Products list is required.")
            .Must(products => products != null && products.Count > 0)
            .WithMessage("At least one product is required.");
    }
}
