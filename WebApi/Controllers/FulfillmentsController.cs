using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.Fulfillments.Commands;
using Core.Application.Mediatr.Fulfillments.Queries;
using Core.Application.Models.Fulfillments;
using Core.Application.Models;

namespace WebApi.Controllers;
#if DISABLED
[Route("api/fulfillments")]
[ApiController]

public class FulfillmentsController : ApiControllerBase
    {
        [HttpGet("{storeUid}")]
        public async Task<ActionResult<FulfillmentDetailsResponse>> GetFulfillment(string storeUid)
        {
            var res = await Mediator.Send(new GetFulfillmentQuery() { StoreUid = storeUid });
            return Ok(res);
        }

        [HttpPost]
        public async Task<ActionResult<UidBaseResponse>> CreateFulfillment(CreateFulfillmentCommand request)
        {
            var uid = await Mediator.Send(request);
            return Ok(new UidBaseResponse() { Uid = uid });
        }

        [HttpPut]
        public async Task<ActionResult> UpdateFulfillment(UpdateFulfillmentCommand request)
        {
            await Mediator.Send(request);
            return Ok();
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult> DeleteFulfillment(string uid)
        {
            await Mediator.Send(new DeleteFulfillmentCommand() { Uid = uid });
            return NoContent();
        }
    }
#endif