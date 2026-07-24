using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendMail(EmailParamsDto emailParamsDto, bool includeAttachments = false);
        Task SendOrderConfirmationEmailsAsync(Order order);
        Task SendOrderShippedEmailAsync(Order order, List<OrderProductAffiliate> shippedItems, string trackingNumber, string shippingProvider);
        Task SendOrderCountdownExpiredEmailAsync(Order order, OrderProductAffiliate orderItem);
        Task SendOrderRefundedEmailAsync(Order order, OrderProductAffiliate orderItem, decimal refundAmount);
        Task SendOrderReorderedEmailAsync(Order order, OrderProductAffiliate orderItem);
        Task SendRefundRequestToSellerAsync(Order order, OrderProductAffiliate orderItem, decimal refundAmount);
        Task SendRefundRejectedToBuyerAsync(Order order, OrderProductAffiliate orderItem, string sellerReason);
        Task SendRefundDisputedToAdminAsync(RefundDispute refundDispute);
        Task SendRefundResolvedToBuyerAsync(Order order, OrderProductAffiliate orderItem, bool approved, string adminNotes);
    }
}
