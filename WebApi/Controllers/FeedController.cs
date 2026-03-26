using System.Threading.Tasks;
using Core.Application.Mediatr.Feed.Queries;
using Core.Application.Models;
using Core.Application.Models.Post;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

#if DISABLED
public class FeedController : ApiControllerBase
{
    [HttpGet("for-you")]
    public async Task<ActionResult<PagingResponse<PostResponse>>> GetUserForYouFeed()
    {
        var x = await Mediator.Send(new GetUserForYourFeedQuery());
        return Ok(x);
    }
}
#endif