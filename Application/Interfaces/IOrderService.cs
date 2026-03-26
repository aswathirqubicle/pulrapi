using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Application.Models.Orders;
using Core.Domain.Entities;

namespace Core.Application.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Gets a paginated list of orders for the current user, either as a buyer or a seller.
    /// Implements data isolation for sellers.
    /// </summary>
    Task<PagingResponse<OrderResponse>> GetUserOrdersAsync(string userId, int pageNumber, int pageSize, bool checkProcessingOnly, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a paginated list of orders for a specific store/seller.
    /// Implements data isolation for the seller.
    /// </summary>
    Task<PagingResponse<OrderResponse>> GetAllOrdersBySellerAsync(string sellerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets details for a specific order by UID.
    /// Implements data isolation for sellers.
    /// </summary>
    Task<OrderDetailsResponse> GetOrderDetailsAsync(string userId, string orderUid, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that the seller owns the specified order item.
    /// Throws ForbiddenException if the seller does not own the item.
    /// </summary>
    Task<OrderProductAffiliate> ValidateSellerOwnershipAsync(string sellerUserId, string itemUid, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the status of multiple order items (mark as shipped).
    /// Automatically updates the parent order status based on all items' statuses.
    /// </summary>
    Task<bool> UpdateOrderItemsStatusAsync(string sellerUserId, List<string> itemUids, string trackingNumber, string shippingProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms delivery of multiple order items.
    /// Automatically updates the parent order status based on all items' statuses.
    /// </summary>
    Task<bool> ConfirmOrderItemsDeliveryAsync(string buyerUserId, List<string> itemUids, CancellationToken cancellationToken);
}
