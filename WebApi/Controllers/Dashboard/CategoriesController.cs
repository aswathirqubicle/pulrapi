using System.Threading.Tasks;
using Dashboard.Application.Mediatr.Categories.Commands.Create;
using Dashboard.Application.Mediatr.Categories.Commands.Update;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Dashboard;
#if DISABLED
public class CategoriesController : DashboardApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var x = await Mediator.Send(command);
        return Ok(x);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryCommand command)
    {
        var x = await Mediator.Send(command);
        return Ok(x);
    }
}
#endif