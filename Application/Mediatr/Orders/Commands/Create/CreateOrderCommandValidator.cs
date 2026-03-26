using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Orders.Commands.Create;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Orders.Commands.Create;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateOrderCommandValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;


        RuleFor(x => x.PaymentMethod).MustAsync(async (paymentMethodKey, cancellationToken) =>
        {
            var paymentMethod = await _dbContext.PaymentMethods.SingleOrDefaultAsync(x => x.Key == paymentMethodKey.ToString());
            return paymentMethod != null;
        }).WithMessage(x => $"Payment method with key '{x.PaymentMethod}' does not exist.");

        RuleFor(x => x.CurrencyUid).MustAsync(async (currencyUid, cancellationToken) =>
        {
            var currency = await _dbContext.Currencies.SingleOrDefaultAsync(x => x.Uid == currencyUid);
            return currency != null;
        }).WithMessage(x => $"Currency with uid '{x.CurrencyUid}' does not exist.");

        When(order => order.PaymentMethod == PaymentMethodEnum.CreditCard, () =>
        {
            //RuleFor(order => order.CardDetails).NotNull().WithMessage(x => $"Card details not provided.");
            RuleFor(order => order.CardDetails.CardNumber).CreditCard().WithMessage(x => $"Card number not valid.");
            RuleFor(order => order.CardDetails.CardCvc).MinimumLength(3).MaximumLength(4).Matches("^[0-9]*$").WithMessage(x => $"Card Cvc not valid.");
            RuleFor(order => order.CardDetails.CardExpDate).GreaterThan(DateTime.UtcNow).WithMessage(x => $"Card expiry date not valid.");
        });

        RuleFor(x => x.Products).Must(products =>
        {
            return products != null && products.Count > 0;
        }).WithMessage(x => $"At least one product must be added.");

    }
}

