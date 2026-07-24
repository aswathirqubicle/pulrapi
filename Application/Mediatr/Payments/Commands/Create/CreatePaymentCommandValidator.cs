using FluentValidation;

namespace Core.Application.Mediatr.Payments.Commands.Create;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.");

        // Standard checkout requires products; exchange difference payments do not.
        When(x => !x.IsExchange, () =>
        {
            RuleFor(x => x.Products)
                .NotNull()
                .WithMessage("Products list is required.")
                .Must(products => products != null && products.Count > 0)
                .WithMessage("At least one product is required.");
        });

        // Exchange difference payments require the exchanged items instead.
        When(x => x.IsExchange, () =>
        {
            RuleFor(x => x.ExchangeItems)
                .NotNull()
                .WithMessage("Exchange items are required for an exchange payment.")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("At least one exchange item is required.");

            RuleForEach(x => x.ExchangeItems).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductOrderUid)
                    .NotEmpty()
                    .WithMessage("ProductOrderUid is required for each exchange item.");

                item.RuleFor(i => i.NewVariantCombinationUid)
                    .NotEmpty()
                    .WithMessage("NewVariantCombinationUid is required for each exchange item.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero for each exchange item.");
            });
        });
    }
}
