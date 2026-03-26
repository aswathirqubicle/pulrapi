using System;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Constants;
using Core.Application.Mediatr.Search.Commands;
using Core.Application.Mediatr.Search.Queries;
using Core.Application.Models.Search;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MediatR;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] SearchRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if(request.Type != "top" && string.IsNullOrWhiteSpace(request.Term))
        {
            return BadRequest("Search term is required for non-top searches");
        }

        try
        {
            // Save search term to history if provided
            //if (!string.IsNullOrWhiteSpace(request.Term))
            //{
            //    await _mediator.Send(new SaveSearchHistoryCommand 
            //    { 
            //        Term = request.Term,
            //        Type = SearchHistoryType.SearchTerm
            //    });
            //}

            return request.Type.ToLower() switch
            {
                "top" => Ok(await _mediator.Send(new GetTopPostsQuery
                {
                    SearchTerm = request.Term,
                    Page = request.Page,
                    PageSize = request.PageSize
                })),
                "posts" => Ok(await _mediator.Send(new GetPostsSearchQuery
                {
                    SearchTerm = request.Term,
                    Page = request.Page,
                    PageSize = request.PageSize
                })),
                "users" => Ok(await _mediator.Send(new GetUsersSearchQuery
                {
                    SearchTerm = request.Term,
                    Page = request.Page,
                    PageSize = request.PageSize
                })),
                "tags" => Ok(await _mediator.Send(new GetTagsSearchQuery
                {
                    SearchTerm = string.IsNullOrWhiteSpace(request.Term) ? request.Term :
                    (request.Term.StartsWith('#')) ? request.Term: request.Term.TrimStart('#'),
                    Page = request.Page,
                    PageSize = request.PageSize
                })),
                "products" => Ok(await _mediator.Send(new GetProductsSearchQuery
                {
                    SearchTerm = request.Term,
                    Page = request.Page,
                    PageSize = request.PageSize
                })),
                _ => BadRequest("Invalid search type. Must be one of: top, posts, users, tags, products")
            };
        }
        catch (Exception ex)
        {
            // Log the exception here
            return StatusCode(500, $"An error occurred while processing your request:${ex.Message}");
        }
    }

    [HttpGet("query")]
    [AllowAnonymous]
    public async Task<ActionResult<SearchResult>> GetSearchResults([FromQuery] string query)
    {
        var result = await _mediator.Send(new GetSearchResultsQuery { Query = query });
        return Ok(result);
    }

    [HttpGet("suggestions")]
    [Authorize(Roles = PulrRoles.User)]
    public async Task<ActionResult<List<UserSuggestionDto>>> GetUserSuggestions([FromQuery] string term)
    {
        var suggestions = await _mediator.Send(new GetUserSuggestionsQuery { SearchTerm = term });
        return Ok(suggestions);
    }

    [HttpPost("click")]
    [Authorize(Roles = PulrRoles.User)]
    public async Task<ActionResult<SearchHistoryResponseDto>> SaveClick([FromBody] SaveSearchHistoryCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("history")]
    [Authorize(Roles = PulrRoles.User)]
    public async Task<ActionResult<List<SearchHistoryResponseDto>>> GetUserSearchHistory()
    {
        var histories = await _mediator.Send(new GetSearchHistoryQuery());
        return Ok(histories);
    }
    
    [HttpDelete("history/{id}")]
    [Authorize(Roles = PulrRoles.User)]
    public async Task<IActionResult> DeleteSearchTerm(string id)
    {
        await _mediator.Send(new DeleteSearchHistoryItemCommand { Id = id });
        return NoContent();
    }

    [HttpDelete("history")]
    [Authorize(Roles = PulrRoles.User)]
    public async Task<IActionResult> DeleteAllSearchHistory()
    {
        await _mediator.Send(new DeleteAllSearchHistoryCommand());
        return NoContent();
    }
}