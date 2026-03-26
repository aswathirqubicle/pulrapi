using System.Threading.Tasks;
using Core.Application.Models;
using Core.Application.Models.Stores;
using Dashboard.Application.Mediatr.Stores.Commands.Create;
using Dashboard.Application.Mediatr.Stores.Commands.Delete;
using Dashboard.Application.Mediatr.Stores.Commands.Update;
using Dashboard.Application.Mediatr.Stores.Commands.Update.AvatarImage;
using Dashboard.Application.Mediatr.Stores.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Dashboard;
#if DISABLED
public class StoresController : DashboardApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagingResponse<StoreResponse>>> GetStores([FromQuery] GetStoresQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [HttpGet("{storeUid}")]
    public async Task<ActionResult<StoreDetailsResponse>> GetStoreDetails(string storeUid)
    {
        var query = new GetStoresDetailsQuery { StoreUid = storeUid };
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateStore([FromBody] CreateStoreCommand command)
    {
        var storeUid = await Mediator.Send(command);
        return Ok(new UidBaseResponse() { Uid = storeUid });
    }

    [HttpPut]
    public async Task<ActionResult> UpdateStore(UpdateStoreCommand request)
    {
        await Mediator.Send(request);
        return Ok();
    }

    [HttpPut("avatar-image")]
    public async Task<ActionResult<UpdateStoreAvatarImageResponse>> UpdateAvatarImage([FromForm] UpdateStoreAvatarImageCommand command)
    {
        var newImageUrl = await Mediator.Send(command);
        return Ok(new UpdateStoreAvatarImageResponse() { NewImageUrl = newImageUrl });
    }

    [HttpDelete("{uid}")]
    public async Task<ActionResult> DeleteStore(string uid)
    {
        await Mediator.Send(new DeleteStoreCommand() { StoreUid = uid });
        return NoContent();
    }
}
#endif