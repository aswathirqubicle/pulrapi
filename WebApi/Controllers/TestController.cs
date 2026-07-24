using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Core.Application.Constants;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Exceptions;
using Core.Domain.Entities;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TestController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TestController> _logger;
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public TestController(
            UserManager<User> userManager,
            ILogger<TestController> logger,
            IMediator mediator,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _logger = logger;
            _mediator = mediator;
            _env = env;
            _configuration = configuration;
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            _logger.LogInformation("Ping endpoint called at {Time}", DateTime.UtcNow);
            return Ok("Pong");
        }

#if DEBUG
        // The destructive dev tooling below is compiled ONLY in local DEBUG builds.
        // Deployment/publish uses the Release configuration, so these endpoints do not
        // exist in any deployed binary regardless of the runtime ASPNETCORE_ENVIRONMENT
        // value (which the deployment config forces to "Development"). The runtime
        // IsDevelopment()/passcode checks below are kept purely as defense-in-depth.

        /// <summary>
        /// [DEV ONLY] Hard-deletes ALL posts for a given user, including their AWS S3 media files.
        /// Requires a valid DevAccess security key. No login required.
        /// Returns 404 when called outside a Development environment.
        /// </summary>
        [HttpDelete("delete-all-user-posts")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteAllUserPosts(
            [FromBody] DeleteAllUserPostsRequest request,
            CancellationToken cancellationToken)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var expectedKey = _configuration[DevAccessConstants.PasscodeConfigKey];
            if (string.IsNullOrEmpty(expectedKey) || request.SecurityKey != expectedKey)
            {
                _logger.LogWarning(
                    "[DevTool] Unauthorized attempt to call delete-all-user-posts. " +
                    "UserIdentifier={UserIdentifier}",
                    request.UserIdentifier);
                return Unauthorized(new { message = "Invalid security key." });
            }

            _logger.LogWarning(
                "[DevTool] delete-all-user-posts called. UserIdentifier={UserIdentifier}",
                request.UserIdentifier);

            try
            {
                var result = await _mediator.Send(
                    new DeleteAllUserPostsCommand { UserIdentifier = request.UserIdentifier },
                    cancellationToken);

                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[DevTool] delete-all-user-posts failed. UserIdentifier={UserIdentifier}",
                    request.UserIdentifier);
                return StatusCode(500, new { message = "Delete failed.", details = ex.Message });
            }
        }

        /// <summary>
        /// [DEV ONLY] Hard-deletes ALL posts in the database, including their AWS S3 media files.
        /// Requires a valid DevAccess security key. No login required.
        /// Returns 404 when called outside a Development environment.
        /// </summary>
        [HttpDelete("delete-all-posts")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteAllPosts(
            [FromBody] DeleteAllPostsRequest request,
            CancellationToken cancellationToken)
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var expectedKey = _configuration[DevAccessConstants.PasscodeConfigKey];
            if (string.IsNullOrEmpty(expectedKey) || request.SecurityKey != expectedKey)
            {
                _logger.LogWarning("[DevTool] Unauthorized attempt to call delete-all-posts.");
                return Unauthorized(new { message = "Invalid security key." });
            }

            _logger.LogWarning("[DevTool] delete-all-posts called — ALL posts will be permanently deleted.");

            try
            {
                var result = await _mediator.Send(new DeleteAllPostsCommand(), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DevTool] delete-all-posts failed.");
                return StatusCode(500, new { message = "Delete failed.", details = ex.Message });
            }
        }
#endif
    }

#if DEBUG
    public sealed class DeleteAllUserPostsRequest
    {
        /// <summary>Email or username of the user whose posts should be deleted.</summary>
        public string UserIdentifier { get; set; }

        /// <summary>Must match DevAccess:Passcode in appsettings.development.json.</summary>
        public string SecurityKey { get; set; }
    }

    public sealed class DeleteAllPostsRequest
    {
        /// <summary>Must match DevAccess:Passcode in appsettings.development.json.</summary>
        public string SecurityKey { get; set; }
    }
#endif
}
