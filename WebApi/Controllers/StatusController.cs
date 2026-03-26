using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using shortid;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Categories.Queries;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers;

public class StatusController : ApiControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly IApplicationDbContext _context;

    public StatusController(ILogger<StatusController> logger, IApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Status: Works.");
        return Ok("Works.");
    }


    [AllowAnonymous]
    [HttpGet("categories")]
    public  async Task<IActionResult> GetCategories()
    {
        var cat = await Mediator.Send(new GetCategoriesQuery());
        return Ok(cat);
    }

    [AllowAnonymous]
    [HttpGet("bucket-objects")]
    public IActionResult GetBucketObjects()
    {
        //await fileUploadService.ListFilesInBucket("purl-media-files", "/");
        return Ok();
    }
}

