using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Mediatr.Categories.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

#if DISABLED
public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories([FromQuery] string search)
    {
        var result = await Mediator.Send(new GetCategoriesQuery { Search = search });
        return Ok(result);
    }
}

#endif