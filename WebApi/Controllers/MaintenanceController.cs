using System;
using System.Threading.Tasks;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers;
#if DISABLED
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IPostPurgeService _postPurgeService;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(IPostPurgeService postPurgeService, ILogger<MaintenanceController> logger)
        {
            _postPurgeService = postPurgeService;
            _logger = logger;
        }

        // For testing only: trigger the purge job manually
        // Consider securing this endpoint in non-dev environments
        [HttpPost("purge-posts")] 
        [AllowAnonymous]
        public async Task<IActionResult> PurgeExpiredDeletedPosts()
        {
            try
            {
                await _postPurgeService.PurgeExpiredDeletedPostsAsync();
                return Ok(new { message = "Post purge triggered." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering post purge");
                return StatusCode(500, new { message = "Error triggering post purge." });
            }
        }
    }

#endif