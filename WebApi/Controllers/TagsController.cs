using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.Tag.Queries;
using System.Collections.Generic;

namespace WebApi.Controllers
{
    public class TagsController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet("popular")]
        public async Task<ActionResult<List<string>>> GetPopularTags()
        {
            var res = await Mediator.Send(new GetPopularTagsQuery());
            return Ok(res);
        }

    }
}
