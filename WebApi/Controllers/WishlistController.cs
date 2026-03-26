using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Wishlist.Commands;
using Core.Application.Mediatr.Wishlist.Queries;
using Core.Application.Models.Wishlist;

namespace WebApi.Controllers
{
    [Route("api/wishlist")]
    [ApiController]
    [Authorize(Roles = PulrRoles.User)]
    public class WishlistController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<WishlistResponse>> GetWishlist()
        {
            var result = await Mediator.Send(new GetWishlistQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<WishlistProductResponse>> AddToWishlist([FromBody] AddToWishlistCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{productUid}")]
        public async Task<ActionResult> RemoveFromWishlist(string productUid, [FromQuery] string productVariantCombinationUid = null)
        {
            await Mediator.Send(new RemoveFromWishlistCommand
            {
                ProductUid = productUid,
                ProductVariantCombinationUid = productVariantCombinationUid
            });
            return Ok();
        }

        [HttpPost("move-to-bag")]
        public async Task<ActionResult<Core.Application.Models.BagItems.BagProductResponse>> MoveFromWishlistToBag([FromBody] MoveFromWishlistToBagCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}

