using Core.Application.Mediatr.Onboarding.Commands;
using Core.Application.Mediatr.Onboarding.Queries;
using Core.Application.Models.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    public class OnboardingController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet("preferences/all")]
        public async Task<ActionResult<VibesResponse>> GetAllPreferences()
        {
            var res = await Mediator.Send(new GetAllOnboardingPreferencesQuery());
            return Ok(res);
        }

        [HttpGet("preferences")]
        public async Task<ActionResult<OnboardingPreferencesResponse>> GetPreferences()
        {
            var res = await Mediator.Send(new GetMyOnboardingPreferencesQuery());
            return Ok(res);
        }

        [HttpPut("preferences")]
        public async Task<ActionResult> UpdatePreferences([FromBody]OnboardingPreferencesCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [HttpPost("who-to-follow")]
        public async Task<ActionResult<OnboardingWhoToFollowResponse>> WhoToFollow([FromBody] GetOnboardingWhoToFollowQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        // [AllowAnonymous]
        // [HttpGet("vibes/all")]
        // public async Task<ActionResult<VibesResponse>> GetAllVibes()
        // {
        //     var res = await Mediator.Send(new GetAllVibesQuery());
        //     return Ok(res);
        // }

        // [HttpGet("vibes")]
        // public async Task<ActionResult<VibesResponse>> GetMyVibes()
        // {
        //     var res = await Mediator.Send(new GetMyVibesQuery());
        //     return Ok(res);
        // }

        [HttpPut("vibes")]
        public async Task<ActionResult> UpdateVibes([FromBody] UpdateVibesCommand command)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { message = string.Join("; ", errors) });
            }

            await Mediator.Send(command);
            return Ok();
        }
    }
}
