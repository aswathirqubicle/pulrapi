using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Core.Application.Mediatr.Orders.Commands.RefundInitiate;
using Core.Application.Mediatr.Orders.Commands.RefundApprove;
using Core.Application.Mediatr.Orders.Commands.RefundReject;
using Core.Application.Mediatr.Orders.Commands.UpdateShippingDetails;
using Core.Application.Mediatr.Orders.Queries.GetSellerRefundDisputeDetail;
using Core.Application.Mediatr.Orders.Queries.GetOrderRefundRequests;
using Core.Application.Mediatr.Orders.Queries.GetRefundRequestsList;
using Core.Application.Mediatr.Orders.Queries.GetShippingDetails;
using Core.Application.Mediatr.Orders.Queries;
using Core.Application.Models;
using Core.Application.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ApiControllerBase
{
    /// <summary>
    /// Get order history for the logged-in user.
    /// Returns paginated list of all orders placed by the current user.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult> GetUserOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] bool checkProcessingOnly = false)
    {
        var res = await Mediator.Send(new GetUserOrdersQuery 
        { 
            PageNumber = pageNumber, 
            PageSize = pageSize,
            CheckProcessingOnly = checkProcessingOnly
        });

        if (checkProcessingOnly)
        {
            return Ok(res.HasProcessingOrders);
        }

        return Ok(res);
    }




    /// <summary>
    /// Get a specific order by UID for the logged-in user.
    /// </summary>
    [HttpGet("{uid}")]
    [Authorize]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrder(string uid)
    {
        var res = await Mediator.Send(new GetOrderQuery { Uid = uid });
        return Ok(res);
    }

    /// <summary>
    /// Get all orders for a specific store (for store owners).
    /// </summary>
    [HttpGet("store/{storeUid}")]
    [Authorize]
    public async Task<ActionResult<PagingResponse<OrderResponse>>> GetAllOrdersByStore(string storeUid, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var res = await Mediator.Send(new GetAllOrdersByStoreQuery 
        { 
            StoreUid = storeUid,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(res);
    }

    /// <summary>
    /// Mark one or more order items as shipped (seller only).
    /// Seller provides tracking number and shipping provider for their items.
    /// Only the seller who owns these items can update their status.
    /// </summary>
    [HttpPost("mark-shipped")]
    [Authorize]
    public async Task<ActionResult> MarkAsShipped([FromBody] Core.Application.Mediatr.Orders.Commands.UpdateOrderItemStatus.UpdateOrderItemStatusCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = "Order items marked as shipped successfully." });
    }

    /// <summary>
    /// Get shipping status for order items. Both buyer and seller can call this.
    /// Buyer sees all items in their order; seller sees only their own items.
    /// Returns IsShipped = false with no details if item not yet shipped.
    /// </summary>
    [HttpGet("mark-shipped")]
    [Authorize]
    public async Task<ActionResult<List<ItemShippingStatusResponse>>> GetShippingDetails([FromQuery] string orderUid, [FromQuery] string itemUid = null)
    {
        var result = await Mediator.Send(new GetShippingDetailsQuery { OrderUid = orderUid, ItemUid = itemUid });
        return Ok(result);
    }

    /// <summary>
    /// Update shipping details for already-shipped items (seller only).
    /// Updates tracking number, shipping provider, and replaces proof images.
    /// </summary>
    [HttpPut("mark-shipped")]
    [Authorize]
    public async Task<ActionResult> UpdateShippingDetails([FromBody] UpdateShippingDetailsCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = "Shipping details updated successfully." });
    }

    /// <summary>
    /// Confirm delivery of one or more order items (buyer only).
    /// Buyer confirms they have received these specific items.
    /// </summary>
    [HttpPost("confirm-delivery")]
    [Authorize]
    public async Task<ActionResult> ConfirmDelivery([FromBody] ConfirmDeliveryRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.ConfirmOrderItemDelivery.ConfirmOrderItemDeliveryCommand
        {
            ItemUids = request.ItemUids
        };
        
        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = "Order items delivery confirmed successfully." });
    }

    /// <summary>
    /// Cleanup a failed order by soft-deleting it, restoring product quantities,
    /// and moving items back to the user's bag.
    /// </summary>
    [HttpPost("failed-cleanup")]
    [Authorize]
    public async Task<ActionResult> FailedCleanup([FromBody] FailedOrderCleanupRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.FailedOrderCleanup.FailedOrderCleanupCommand
        {
            OrderUid = request.OrderUid
        };

        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = "Failed order cleaned up and items returned to bag." });
    }

    /// <summary>
    /// Refund one or more failed order items. Credits amount to buyer wallet immediately.
    /// </summary>
    [HttpPost("refund")]
    [Authorize]
    public async Task<ActionResult> RefundOrderItem([FromBody] RefundOrderRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.RefundOrder.RefundOrderCommand
        {
            ItemUids = request.ItemUids,
            Confirmed = request.Confirmed
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Buyer requests a refund for one or more delivered order items.
    /// Accepts batch of line items with individual reasons and evidence files.
    /// </summary>
    [HttpPost("refund/request")]
    [Authorize]
    public async Task<ActionResult<RefundInitiateResponse>> RefundRequest([FromBody] RefundInitiateCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Seller approves a refund request.
    /// Triggers Stripe refund to buyer's original payment method.
    /// </summary>
    [HttpPost("refund/approve")]
    [Authorize]
    public async Task<ActionResult<RefundApproveResponse>> RefundApprove([FromBody] RefundApproveCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Seller rejects a refund request.
    /// Provides a reason and optional supporting media files.
    /// </summary>
    [HttpPost("refund/reject")]
    [Authorize]
    public async Task<ActionResult<RefundRejectResponse>> RefundReject([FromBody] RefundRejectCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// View all refund requests for an order (buyer or seller).
    /// Returns full details including evidence, return address, and reasons.
    /// </summary>
    [HttpGet("refund/viewRequest/{orderUid}")]
    [Authorize]
    public async Task<ActionResult<List<OrderRefundRequestDto>>> ViewRefundRequest(string orderUid)
    {
        var query = new Core.Application.Mediatr.Orders.Queries.GetOrderRefundRequests.GetOrderRefundRequestsQuery
        {
            OrderUid = orderUid
        };
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// List all refund requests for the authenticated user (buyer or seller).
    /// Lightweight summary: order, product, status, amount, date.
    /// </summary>
    [HttpGet("refund/requests")]
    [Authorize]
    public async Task<ActionResult<List<RefundRequestSummaryDto>>> GetRefundRequestsList()
    {
        var query = new Core.Application.Mediatr.Orders.Queries.GetRefundRequestsList.GetRefundRequestsListQuery();
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Reorder one or more failed order items. Restarts countdown (one-time only per item).
    /// </summary>
    [HttpPost("reorder")]
    [Authorize]
    public async Task<ActionResult> Reorder([FromBody] ReorderRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.Reorder.ReorderCommand
        {
            ItemUids = request.ItemUids
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Extend delivery countdown by 72 hours for shipped items (buyer only).
    /// Can only be used once per item, after countdown has expired.
    /// </summary>
    [HttpPost("extend-delivery")]
    [Authorize]
    public async Task<ActionResult> ExtendDelivery([FromBody] ExtendDeliveryRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.ExtendDelivery.ExtendDeliveryCommand
        {
            ItemUids = request.ItemUids
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }

}

// Request models
public class FailedOrderCleanupRequest
{
    public string OrderUid { get; set; }
}


public class ConfirmDeliveryRequest
{
    public List<string> ItemUids { get; set; } = new();
}

public class RefundOrderRequest
{
    public List<string> ItemUids { get; set; } = new();
    public bool Confirmed { get; set; }
}

public class ReorderRequest
{
    public List<string> ItemUids { get; set; } = new();
}

public class ExtendDeliveryRequest
{
    public List<string> ItemUids { get; set; } = new();
}

public class RefundRejectRequest
{
    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; }

    public List<string> MediaFileUids { get; set; } = new List<string>();
}
