using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Mediatr.ShippingDetails.Commands;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models;

namespace WebApi.Controllers;
// #if DISABLED
[Route("api/user-shipping-details")]
public class UserShippingDetailsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagingResponse<ShippingDetailsResponse>>> GetShippingDetails()
    {
        var res = await Mediator.Send(new GetShippingDetailsQuery());
        return Ok(res);
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult<ShippingDetailsResponse>> GetShippingAddress(string uid)
    {
        var res = await Mediator.Send(new GetShippingAddressQuery { Uid = uid });
        return Ok(res);
    }

    // [HttpGet("default")]
    // public async Task<ActionResult<ShippingDetailsResponse>> GetDefaultShippingAddress()
    // {
    //     var res = await Mediator.Send(new GetDefaultShippingAddressQuery());
    //     return Ok(res);
    // }

    [HttpPost]
    public async Task<ActionResult<ShippingDetailsResponse>> CreateShippingAddress([FromBody] CreateShippingAddressCommand command)
    {
        var res = await Mediator.Send(command);
        return Ok(res);
    }

    [HttpPut]
    public async Task<ActionResult<ShippingDetailsResponse>> UpdateShippingDetails([FromBody] UpdateMyShippingDetailsCommand request)
    {
        var res = await Mediator.Send(request);
        return Ok(res);
    }

    // [HttpPatch("{uid}")]
    // public async Task<ActionResult<NoContentResult>> UpdateShippingDetails(string uid)
    // {
    //     await Mediator.Send(new SetDefaultShippingAddressCommand {Uid = uid});
    //     return NoContent();
    // }

    [HttpDelete("{uid}")]
    public async Task<IActionResult> DeleteShippingDetails(string uid)
    {
        await Mediator.Send(new DeleteMyShippingDetailsCommand { Uid = uid });
        return NoContent();
    }
}
// #endif