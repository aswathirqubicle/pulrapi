using Core.Application.Mediatr.Stories.Commands.Create;
using Core.Application.Mediatr.Stories.Commands.Delete;
using Core.Application.Mediatr.Stories.Commands.MarkStoryAsSeen;
using Core.Application.Mediatr.Stories.Commands.ShareCollectionAsStory;
using Core.Application.Mediatr.Stories.Commands.SharePostAsStory;
using Core.Application.Mediatr.Stories.Commands.ShareProductAsStory;
using Core.Application.Mediatr.Stories.Commands.ToggleLike;
using Core.Application.Mediatr.Stories.Queries;
using Core.Application.Models.Stories;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers;

public class StoriesController : ApiControllerBase
{
    
    [AllowAnonymous]
    [HttpGet("feed")]
    public async Task<ActionResult<List<ProfileWithStoriesResponse>>> GetFeedStories([FromQuery] int limit = 10, [FromQuery] int pageNumber = 1)
    {
        var res = await Mediator.Send(new GetFeedStoriesQuery { Limit = limit, PageNumber = pageNumber });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ProfileWithStoriesResponse>> GetAccountStories([FromQuery] bool isStore, [FromQuery]string entityUid)
    {
        var res = await Mediator.Send(new GetAccountStoriesQuery() { IsStore = isStore, EntityUid = entityUid  });
        return Ok(res);
    }

    [HttpGet("for-you")]
    public Task<IActionResult> GetForYouStories()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult<ProfileWithStoriesResponse>> GetSingleStory(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
                uid,
                new SafeUidAttribute(allowNullValue: true, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "uid",
                statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        var result = await Mediator.Send(new GetSingleStoryQuery { Uid = uid });
        return Ok(result);
    }

    [Authorize]
    [HttpPost("share-post-as-story")]
    public async Task<ActionResult<StoryResponse>> ShareYourPostAsStory([FromBody] SharePostAsStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [Authorize]
    [HttpPost("share-product-as-story")]
    public async Task<ActionResult<StoryResponse>> ShareProductAsStory([FromBody] ShareProductAsStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [Authorize]
    [HttpPost("share-collection-as-story")]
    public async Task<ActionResult<StoryResponse>> ShareCollectionAsStory([FromBody] ShareCollectionAsStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<StoryResponse>> CreateStory([FromBody] CreateStoryCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [Authorize]
    [HttpDelete("{uid}")]
    public async Task<IActionResult> DeleteStory(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
                uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "uid",
                statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        await Mediator.Send(new DeleteStoryCommand { Uid = uid });
        return NoContent();
    }

    [Authorize]
    [HttpPut("{uid}/toggle-like")]
    public async Task<ActionResult<StoryToggleLikeResponse>> ToggleLikeStory(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
                uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "uid",
                statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        var res = await Mediator.Send(new StoryToggleLikeCommand { StoryUid = uid });
        return Ok(res);
    }

    [Authorize]
    [HttpPost("mark-as-seen")]
    public async Task<IActionResult> MarkStoryAsSeen([FromBody] MarkStoryAsSeenCommand command)
    {
        await Mediator.Send(command);
        return Ok(new
        {
            StoryUid = command.StoryUid,
            Message = "Story marked as seen successfully.",
            Success = true
        });
    }
}