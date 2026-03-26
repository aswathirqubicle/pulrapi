using Core.Application.Mediatr.Messages.Commands;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    public class MessagesController : ApiControllerBase
    {
        [HttpPost("reply-to-story")]
        public async Task<IActionResult> ReplyToStory([FromBody] ReplyToStoryCommand request)
        {
            await Mediator.Send(request);
            return Ok();
        }
    }
}
