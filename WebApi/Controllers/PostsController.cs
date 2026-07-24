using Core.Application.Constants;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Commands;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Mediatr.Reports.Commands;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Application.Models.Reports;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using Core.Application.Models.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers
{
    public class PostsController(IApplicationDbContext dbContext, IWebHostEnvironment environment) : ApiControllerBase
    {
        private readonly IApplicationDbContext _dbContext = dbContext;
        private readonly IWebHostEnvironment _environment = environment;

        [AllowAnonymous]
        [HttpGet("hashtags")]
        public async Task<ActionResult<List<HashtagResponse>>> GetHashtags([FromQuery] string searchTerm, [FromQuery] int? limit)
        {
            var res = await Mediator.Send(new GetHashtagsQuery { SearchTerm = searchTerm, Limit = limit });
            return Ok(res);
        }

        [AllowAnonymous]
        [HttpGet("{uid}")]
        public async Task<ActionResult<PostDetailsResponse>> GetPost(string uid, [FromQuery] string currencyCode, [FromQuery] ProductTypeEnum? productType)
        {
            if (!IsValidUid(uid)) return BadRequest(new { error = "Invalid post UID format." });

            var res = await Mediator.Send(new GetPostQuery { Uid = uid, CurrencyCode = currencyCode, ProductType = productType });
            return Ok(res);
        }

        [HttpPost]
        public async Task<ActionResult<PostDetailsResponse>> CreatePost(CreatePostCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult> DeletePost(string uid)
        {
            if (!IsValidUid(uid)) return BadRequest(new { error = "Invalid post UID format." });

            await Mediator.Send(new DeletePostCommand() { Uid = uid });
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<PostResponse>>> GetPosts([FromQuery] GetPostsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [HttpGet("following")]
        public async Task<ActionResult<PagingResponse<PostResponse>>> GetUserFollowingFeed([FromQuery] GetUserFollowingFeedQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/toggle-like")]
        public async Task<ActionResult<ToggleLikePostResponse>> ToggleLikePost(string uid)
        {
            if (!IsValidUid(uid)) return BadRequest(new { error = "Invalid post UID format." });

            var res = await Mediator.Send(new ToggleLikePostCommand() { PostUid = uid });
            return Ok(res);
        }

        [HttpPost("share")]
        public async Task<ActionResult<PostResponse>> SharePost(SharePostCommand command)
        {
            var postResponse = await Mediator.Send(command);
            return Ok(postResponse);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("{uid}/report")]
        public async Task<ActionResult<ReportResponse>> ReportPost(string uid)
        {
            if (!IsValidUid(uid)) return BadRequest(new { error = "Invalid post UID format." });

            // First check if it's a story
            var story = await _dbContext.Stories.FirstOrDefaultAsync(s => s.Uid == uid);
            if (story != null)
            {
                var responseS = await Mediator.Send(new ReportEntityCommand { EntityUid = uid, Type = ReportTypeEnum.Story });
                return Ok(responseS);
            }

            // If not a story, treat it as a post
            var response = await Mediator.Send(new ReportEntityCommand { EntityUid = uid, Type = ReportTypeEnum.Post });
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("similar/{postUid}")]
        public async Task<ActionResult<PagingResponse<PostDetailsResponse>>> GetSimilarPosts(
            string postUid,
            [FromQuery] string currencyCode = null,
            [FromQuery] int maxProductMatches = 3,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeBoughtSimilar = false,
            [FromQuery] bool includeWishlist = false)
        {
            if (!IsValidUid(postUid)) return BadRequest(new { error = "Invalid post UID format." });

            var query = new GetSimilarPostsByTaggedProductsQuery
            {
                PostUid = postUid,
                CurrencyCode = currencyCode,
                MaxProductMatches = maxProductMatches,
                PageNumber = pageNumber,
                PageSize = pageSize,
                IncludeBoughtSimilar = includeBoughtSimilar,
                IncludeWishlist = includeWishlist
            };

            var res = await Mediator.Send(query);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("{uid}/product-tags")]
        public async Task<ActionResult<PostDetailsResponse>> ReplacePostProductTags(string uid, ReplacePostProductTagsCommand command)
        {
            command.PostUid = uid;
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPut("uid")]
        public async Task<ActionResult<PostDetailsResponse>> UpdatePost(UpdatePostCommand command)
        {
            //if (!IsValidUid(uid)) return BadRequest(new { error = "Invalid post UID format." });
            //command.PostUid = uid;
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        private bool IsValidUid(string uid)
        {
            var uidValidationError = this.ValidateWithAttribute(
                uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "uid",
                statusCode: 400);
            return uidValidationError == null;
        }

            /// <summary>
            /// DEVELOPMENT ONLY: Bulk delete posts based on specified criteria
            /// </summary>
            //[AllowAnonymous]
            //[HttpPost("dev/bulk-delete")]
            //public async Task<ActionResult<BulkDeletePostsResponse>> BulkDeletePostsByCriteria(BulkDeletePostsByCriteriaCommand command)
            //{
            //    // Ensure this endpoint only works in Development environment
            //    if (!_environment.IsDevelopment())
            //    {
            //        return BadRequest(new { message = "This endpoint is only available in Development environment." });
            //    }

            //    var res = await Mediator.Send(command);
            //    return Ok(res);
            //}

        }
}