using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.BagItems.Commands;
using Core.Application.Mediatr.BagItems.Queries;
using Core.Application.Models.BagItems;

namespace WebApi.Controllers;

[Route("api/bag-items")]
[ApiController]
[Authorize(Roles = PulrRoles.User)]
public class BagItemsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BagResponse>> GetBagItems()
    {
        var res = await Mediator.Send(new GetBagItemsQuery());
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<BagProductResponse>> AddToBag([FromBody] AddToBagCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{productUid}")]
    public async Task<ActionResult> RemoveFromBag(string productUid, [FromQuery] string productVariantCombinationUid = null)
    {
        await Mediator.Send(new RemoveFromBagCommand
        {
            ProductUid = productUid,
            ProductVariantCombinationUid = productVariantCombinationUid
        });
        return Ok();
    }

    [HttpPut("quantity")]
    public async Task<ActionResult> UpdateBagItemQuantity([FromBody] UpdateBagItemQuantityCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpPost("move-to-wishlist")]
    public async Task<ActionResult<Core.Application.Models.Wishlist.WishlistProductResponse>> MoveFromBagToWishlist([FromBody] MoveFromBagToWishlistCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    // [HttpPut]
    // public async Task<ActionResult> UpdateBagItems([FromBody] UpdateBagItemsCommand request)
    // {
    //     await Mediator.Send(request);
    //     return Ok();
    // }

    // [AllowAnonymous]
    // [HttpPost("calculate-exchange-rates")]
    // public async Task<ActionResult<BagItemsExchangeRatesResponse>> CalculateExchangeRates([FromBody] CalculateExchangeRatesBagItemsCommand request)
    // {
    //     var res = await Mediator.Send(request);
    //     return Ok(res);
    // }
}
