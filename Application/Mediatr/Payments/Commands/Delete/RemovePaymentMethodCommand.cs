using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Payments.Commands.Delete;

public class RemovePaymentMethodCommand : IRequest<bool>
{
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class RemovePaymentMethodCommandHandler : IRequestHandler<RemovePaymentMethodCommand, bool>
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<RemovePaymentMethodCommandHandler> _logger;

    public RemovePaymentMethodCommandHandler(IStripeService stripeService, ILogger<RemovePaymentMethodCommandHandler> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<bool> Handle(RemovePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _stripeService.DetachPaymentMethodAsync(request.PaymentMethodId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment method {PaymentMethodId}: {Message}", request.PaymentMethodId, ex.Message);
            return false;
        }
    }
}
