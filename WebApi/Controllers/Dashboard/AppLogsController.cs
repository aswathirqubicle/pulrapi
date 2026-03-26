using Core.Application.Models;
using Dashboard.Application.Mediatr.AppLogs.Queries;
using Dashboard.Application.Models.AppLogs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApi.Controllers.Dashboard;
#if DISABLED
public class AppLogsController : DashboardApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagingResponse<AppLogResponse>>> GetAppLogs([FromQuery] GetAppLogsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }
}
#endif