using Core.Application.Common;
using Core.Application.Mediatr.Comments.Commands;
using Core.Application.Mediatr.Comments.Queries;
using Core.Application.Models;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers
{
    public class CommentsController : ApiControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("reply")]
        public async Task<ActionResult<CommentResponse>> ReplyToComment([FromBody] ReplyToCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpPut]
        public async Task<ActionResult<CommentResponse>> UpdateComment([FromBody] UpdateCommentCommand command)
        {
            var response = await Mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete("{uid}")]
        public async Task<ActionResult<DeleteCommentResponse>> DeleteComment(string uid)
        {
            var uidValidationError = this.ValidateWithAttribute(
                uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "CommentUid",
                statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            var response = await Mediator.Send(new DeleteCommentCommand { CommentUid = uid });
            return Ok(response);
        }

        [HttpPut("{uid}/toggle-like")]
        public async Task<ActionResult<CommentToggleLikeResponse>> ToggleLike(string uid)
        {
            var uidValidationError = this.ValidateWithAttribute(
            uid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "Uid",
            statusCode: 400);
            if (uidValidationError != null) return uidValidationError;


            var result = await Mediator.Send(new ToggleCommentLikeCommand { Uid = uid });
            if (result == null) return NotFound(new { error = "Comment not found." });
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagingResponse<CommentResponse>>> GetComments([FromQuery] GetCommentsQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }
    }
}