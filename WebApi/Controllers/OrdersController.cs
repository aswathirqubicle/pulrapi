using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<ActionResult<OrderDetailsResponse>> GetOrder(string uid)
    {
        var res = await Mediator.Send(new GetOrderQuery { Uid = uid });
        return Ok(res);
    }

    /// <summary>
    /// Get all orders for a specific store (for store owners).
    /// </summary>
    [HttpGet("store/{storeUid}")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<ActionResult> MarkAsShipped([FromBody] MarkAsShippedRequest request)
    {
        var command = new Core.Application.Mediatr.Orders.Commands.UpdateOrderItemStatus.UpdateOrderItemStatusCommand
        {
            ItemUids = request.ItemUids,
            TrackingNumber = request.TrackingNumber,
            ShippingProvider = request.ShippingProvider
        };
        
        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = "Order items marked as shipped successfully." });
    }

    /// <summary>
    /// Confirm delivery of one or more order items (buyer only).
    /// Buyer confirms they have received these specific items.
    /// </summary>
    [HttpPost("confirm-delivery")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    /// Reorder one or more failed order items. Restarts countdown (one-time only per item).
    /// </summary>
    [HttpPost("reorder")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

public class MarkAsShippedRequest
{
    public List<string> ItemUids { get; set; } = new();
    public string TrackingNumber { get; set; }
    public string ShippingProvider { get; set; }
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
