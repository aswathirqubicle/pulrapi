using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Profiles.Queries;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Mediatr.Stores.Queries;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
#if DISABLED
    [Authorize(Roles = PulrRoles.Administrator + "," + PulrRoles.User)]
    public class StoresController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<StoreResponse>>> GetStores([FromQuery] GetStoresQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("unique-name/{uniqueName}")]
        public async Task<ActionResult<StoreDetailsResponse>> GetStoreByUniqueName(string uniqueName)
        {
            var res = await Mediator.Send(new GetStoreByNameQuery { UniqueName = uniqueName });
            return Ok(res);
        }

        [HttpGet("{uid}")]
        public async Task<ActionResult<StoreDetailsResponse>> GetStoreDetails(string uid)
        {
            var res = await Mediator.Send(new GetStoreDetailsQuery { Uid = uid });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("tagged-on")]
        public async Task<ActionResult<StoreDetailsResponse>> GetTaggedStorePosts([FromQuery] GetTaggedStorePostsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("setup-data")]
        public async Task<ActionResult<StoreSetupDataResponse>> GetSetupData()
        {
            var setupData = await Mediator.Send(new GetStoreSetupDataQuery());
            return Ok(setupData);
        }

        [HttpPost]
        public async Task<ActionResult<UidBaseResponse>> CreateStore(CreateStoreCommand request)
        {
            var res = await Mediator.Send(request);
            return Ok(res);
        }

        [HttpPut]
        public async Task<ActionResult<StoreDetailsResponse>> UpdateStore(UpdateStoreCommand request)
        {
            var res = await Mediator.Send(request);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("edit-bio")]
        public async Task<ActionResult<StoreBioDto>> EditStoreBio([FromBody] UpdateStoreBioCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult> DeleteStore(string uid)
        {
            await Mediator.Send(new DeleteStoreCommand() { Uid = uid });
            return NoContent();
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("avatar-image")]
        public async Task<ActionResult<UpdateStoreAvatarImageResponse>> UpdateAvatarImage([FromForm] UpdateStoreAvatarImageCommand command)
        {
            var newImageUrl = await Mediator.Send(command);
            return Ok(new UpdateStoreAvatarImageResponse() { NewImageUrl = newImageUrl });
        }

        [AllowAnonymous]
        [HttpPost("top-brands")]
        public async Task<ActionResult<List<StoreDetailsResponse>>> TopBrands([FromBody] GetTopBrandsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/toggle-follow")]
        public async Task<ActionResult> ToggleFollow(string uid)
        {
            var res = await Mediator.Send(new ToggleFollowStoreCommand() { StoreUid = uid });
            return Ok(res);
        }

        // Maybe later we need it
        [HttpPut("rating")]
        public async Task<ActionResult> UpdateRating(StoreUpdateRatingCommand request)
        {
            await Mediator.Send(request);
            return Ok();
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/accept-terms")]
        public async Task<ActionResult> AcceptTerms(string uid)
        {
            await Mediator.Send(new AcceptTermsStoreCommand() { StoreUid = uid });
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("{storeUid}/followers")]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetStoreFollowers(string storeUid)
        {
            var res = await Mediator.Send(new GetStoreFollowersQuery() { StoreUid = storeUid });
            return Ok(res);
        }
    }
#endif