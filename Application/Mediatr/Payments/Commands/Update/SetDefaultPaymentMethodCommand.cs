using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Payments.Commands.Update;

public class SetDefaultPaymentMethodCommand : IRequest<bool>
{
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class SetDefaultPaymentMethodCommandHandler : IRequestHandler<SetDefaultPaymentMethodCommand, bool>
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<SetDefaultPaymentMethodCommandHandler> _logger;

    public SetDefaultPaymentMethodCommandHandler(IStripeService stripeService, ILogger<SetDefaultPaymentMethodCommandHandler> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<bool> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _stripeService.SetDefaultPaymentMethodAsync(request.PaymentMethodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId}: {Message}", request.PaymentMethodId, ex.Message);
            return false;
        }
    }
}
