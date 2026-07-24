using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Mediatr.BagItems.Queries;
using Core.Application.Mediatr.Profiles.Commands;
using Core.Application.Mediatr.Profiles.Queries;
using Core.Application.Mediatr.Reports.Commands;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Reports;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize(Roles = PulrRoles.Administrator + "," + PulrRoles.User)]
    public class ProfilesController : ApiControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        public ProfilesController(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }
        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/toggle-follow")]
        public async Task<ActionResult> ToggleFollow(string uid)
        {
            var res = await Mediator.Send(new ProfileToggleFollowCommand() { ProfileUid = uid });
            
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/accept-follow-request")]
        public async Task<ActionResult> AcceptFollowRequest(string uid)
        {
            var res = await Mediator.Send(new AcceptFollowRequestCommand() { RequesterProfileUid = uid });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/reject-follow-request")]
        public async Task<ActionResult> RejectFollowRequest(string uid)
        {
            var res = await Mediator.Send(new RejectFollowRequestCommand() { RequesterProfileUid = uid });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/toggle-follow-request")]
        public async Task<ActionResult> ToggleFollowRequest(string uid)
        {
            var res = await Mediator.Send(new ToggleFollowRequestCommand() { TargetProfileUid = uid });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpGet("follow-requests")]
        public async Task<ActionResult<List<FollowRequestDto>>> GetPendingFollowRequests()
        {
            var res = await Mediator.Send(new GetPendingFollowRequestsQuery());
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("by-username/{username}")]
        public async Task<ActionResult<ProfileDetailsResponse>> GetProfileByUsername(string username)
        {
            var res = await Mediator.Send(new GetProfileQuery { Username = username });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("search-handles/{search}")]
        public async Task<ActionResult<List<string>>> GetHandles(string search)
        {
            var res = await Mediator.Send(new ProfileGetHandlesQuery { Search = search });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpGet("my")]
        public async Task<ActionResult<MyProfileDetailsResponse>> GetMyProfile()
        {
            var res = await Mediator.Send(new ProfileSettingsGetQuery());
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("my")]
        public async Task<ActionResult> UpdateMyProfile(UpdateMyProfileCommand request)
        {
            await Mediator.Send(request);
            return Ok();
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("edit-bio")]
        public async Task<ActionResult<ProfileBioDto>> EditProfileBio([FromBody] UpdateProfileBioCommand command)
        {
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("avatar-image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProfileAvatarImageUpdateResponse>> UpdateAvatarImage([FromForm] ProfileAvatarImageUpdateCommand command)
        {
            var newImageUrl = await Mediator.Send(command);
            return Ok(new ProfileAvatarImageUpdateResponse() { NewImageUrl = newImageUrl });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetProfiles([FromQuery] GetProfilesQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpPost("who-to-follow")]
        public async Task<ActionResult<List<ProfileResponse>>> WhoToFollow([FromBody] ProfilesTopFollowedQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [HttpGet("friends")]
        public async Task<ActionResult> GetFriends([FromQuery] int? count, [FromQuery] string search)
        {
            var result = await Mediator.Send(new GetFriendsQuery
            {
                Count = count ?? 10,
                Search = search
            });
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{profileUid}/followers")]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetProfileFollowers(
            string profileUid,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string search = null)
        {
            var res = await Mediator.Send(new GetProfileFollowersQuery
            {
                ProfileUid = profileUid,
                PageNumber = pageNumber,
                PageSize   = pageSize,
                Search     = search
            });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("{profileUid}/followings")]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetProfileFollowings(
            string profileUid,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var res = await Mediator.Send(new GetProfileFollowingsQuery
            {
                ProfileUid = profileUid,
                PageNumber = pageNumber,
                PageSize   = pageSize
            });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpGet("my/followers")]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetMyFollowers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string search = null)
        {
            var user = await _currentUserService.GetUserAsync();
            var res = await Mediator.Send(new GetProfileFollowersQuery { ProfileUid = user.Profile.Uid, PageNumber = pageNumber, PageSize = pageSize, Search = search });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/report")]
        public async Task<ActionResult<ReportResponse>> ReportProfile(string uid)
        {
            var response = await Mediator.Send(new ReportEntityCommand { EntityUid = uid, Type = ReportTypeEnum.Profile });
            return Ok(response);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("unlink-external-account")]
        public async Task<ActionResult> UnlinkExternalAccount([FromBody] UnlinkExternalAccountCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(new { success = result, message = $"{command.Provider} account unlinked successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred while unlinking the account - {ex}" });
            }
        }

        [AllowAnonymous]
        [HttpGet("most-followed")]
        public async Task<ActionResult<List<MostFollowedProfileDto>>> GetMostFollowed([FromQuery] int limit)
        {
            var res = await Mediator.Send(new GetMostFollowedProfilesQuery { Limit = limit });
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpGet("all-users")]
        public async Task<ActionResult<PagingResponse<ProfileDetailsResponse>>> GetAllUsers([FromQuery] GetAllUsersQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }
    }
}