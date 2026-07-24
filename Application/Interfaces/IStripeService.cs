using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Models.Stripe;

namespace Core.Application.Interfaces;

public interface IStripeService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
    Task<CreateCustomerSessionResponse> CreateCustomerSessionAsync(CreateCustomerSessionRequest request);
    Task<CustomerResponse> GetCustomerAsync();
    Task<IReadOnlyList<PaymentMethodResponse>> GetSavedPaymentMethodsAsync();
    Task<PaymentMethodResponse?> GetPaymentMethodAsync(string paymentMethodId);
    Task<SaveCardResponse> SaveCardAsync(SaveCardRequest request);
    Task<bool> DetachPaymentMethodAsync(string paymentMethodId);
    Task<bool> SetDefaultPaymentMethodAsync(string paymentMethodId);
    Task<SetupIntentResponse> CreateSetupIntentAsync();
    Task<bool> HandleWebhookAsync(string json, string stripeSignature);
    Task<RefundResponse> CreateRefundAsync(RefundRequest request);
    Task<TransferReversalResponse> ReverseTransferAsync(ReverseTransferRequest request);
}
