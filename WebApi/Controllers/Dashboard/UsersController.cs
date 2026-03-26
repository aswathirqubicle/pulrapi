using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Models;
using Core.Application.Models.Users;
using Dashboard.Application.Mediatr.Users.Commands.Login;
using Dashboard.Application.Mediatr.Users.Commands.ToggleSuspendUser;
using Dashboard.Application.Mediatr.Users.Commands.Update;
using Dashboard.Application.Mediatr.Users.Commands.Update.AvatarImage;
using Dashboard.Application.Mediatr.Users.Commands.Verify;
using Dashboard.Application.Mediatr.Users.Queries;
using Dashboard.Application.Mediatr.Users.Users;
using Dashboard.Application.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Dashboard;

#if DISABLED
    [Tags("Dashboard - Users")]
    [Authorize(Roles = PulrRoles.Administrator)]
    public class UsersController : DashboardApiControllerBase
    {
        // TODO
        [HttpGet("{uid}")]
        public async Task<ActionResult<UserDetailsResponse>> GetUser(string uid)
        {
            var res = await Mediator.Send(new GetUserQuery() { Uid = uid });
            return Ok(res);
        }

        [HttpGet]
        public async Task<ActionResult<PagingResponse<UserResponse>>> GetUsers([FromQuery] GetUsersQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [HttpPut]
        public async Task<ActionResult<NoContentResult>> UpdateUser([FromBody] UpdateUserCommand command)
        {
            await Mediator.Send(command);
            return NoContent();
        }

        [HttpPatch("avatar-image/{userId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<UpdateUserAvatarImageResponse>> UpdateAvatarImage(string userId, [FromForm] UpdateUserAvatarImageCommand command)
        {
            command.UserId = userId;
            var profileImageUrl = await Mediator.Send(command);
            return Ok(new UpdateUserAvatarImageResponse() { ProfileImageUrl = profileImageUrl });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(DashboardLoginCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [HttpPatch("toggle-suspend")]
        public async Task<ActionResult<NoContentResult>> ToggleSuspendUser(ToggleSuspendUserCommand toggleSuspendUserCommand)
        {
            await Mediator.Send(toggleSuspendUserCommand);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<NoContentResult>> VerifyUser(string id)
        {
            await Mediator.Send(new VerifyUserCommand { Id = id });
            return NoContent();
        }
    }
}
#endif