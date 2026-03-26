using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Core.Application.Models.BookmarkCollections;
using Core.Application.Models;
using Core.Application.Interfaces;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApi.Utilities;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/collections")]
    [Authorize]
    public class CollectionsController(IBookmarkCollectionService service, ICurrentUserService currentUserService) : ControllerBase
    {
        private readonly IBookmarkCollectionService _service = service;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        [HttpPost]
        public async Task<ActionResult<BookmarkCollectionResponse>> Create([FromBody] CreateBookmarkCollectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { message = string.Join("; ", errors) });
            }

            var user = await _currentUserService.GetUserAsync();
            var result = await _service.CreateCollectionAsync(request.Name, user.Profile.Uid, request.PostId);
            return Ok(result);
        }

        [HttpPut("Uid")]
        public async Task<ActionResult<BookmarkCollectionResponse>> Update([FromBody] UpdateBookmarkCollectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { message = string.Join("; ", errors) });
            }

            var user = await _currentUserService.GetUserAsync();
            var result = await _service.UpdateCollectionAsync(request.Uid, request.Name, user.Profile.Uid);
            return Ok(result);
        }

        [HttpDelete("Uid")]
        public async Task<IActionResult> Delete([FromQuery] string Uid)
        {            
            var uidValidationError = this.ValidateWithAttribute(
                Uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1,validateGuidFormat:true),
                memberName: "Uid",
                statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            var user = await _currentUserService.GetUserAsync();
            await _service.DeleteCollectionAsync(Uid, user.Profile.Uid);
            return NoContent();
        }

        //[HttpPost("Uid/posts")]
        //public async Task<IActionResult> AddPostToCollection([FromBody] AddPostToCollectionRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Values
        //            .SelectMany(v => v.Errors)
        //            .Select(e => e.ErrorMessage)
        //            .ToList();
                
        //        return BadRequest(new { message = string.Join("; ", errors) });
        //    }

        //    var user = await _currentUserService.GetUserAsync();
        //    await _service.AddPostToCollectionAsync(request.PostUid, request.CollectionUid, user.Profile.Uid);
        //    return NoContent();
        //}

        [HttpDelete("Uid/posts/postUid")]
        public async Task<ActionResult<object>> RemovePostFromCollection([FromQuery] string Uid, [FromQuery] string postId)
        {
            var uidValidationError = this.ValidateWithAttribute(
                Uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1,validateGuidFormat:true),
                memberName: "Uid",
                statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            var postIdValidationError = this.ValidateWithAttribute(
                postId,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "postId",
                statusCode: 400);
            if (postIdValidationError != null) return postIdValidationError;

            var user = await _currentUserService.GetUserAsync();
            // Get post info before removal
            var post = await _service.GetPostByUidAsync(postId); // You may need to implement this method in the service
            await _service.RemovePostFromCollectionAsync(postId, Uid, user.Profile.Uid);
            return Ok(new { postId = postId, postImageUrl = post?.MediaFile?.Url });
        }

        [HttpPost("Uid/share")]
        public async Task<IActionResult> ShareCollection([FromBody] ShareCollectionRequest request)
        {
            var collectionUidValidationError = this.ValidateWithAttribute(
                request.CollectionUid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "CollectionUid",
                statusCode: 400);
            if (collectionUidValidationError != null) return collectionUidValidationError;

            var profileUidValidationError = this.ValidateWithAttribute(
                request.TargetProfileUid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
                memberName: "TargetProfileUid",
                statusCode: 400);
            if (profileUidValidationError != null) return profileUidValidationError;
            var user = await _currentUserService.GetUserAsync();
            await _service.ShareCollectionWithUserAsync(request.CollectionUid, user.Profile.Uid, request.TargetProfileUid);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("Uid")]
        public async Task<ActionResult<BookmarkCollectionResponse>> GetCollection([FromQuery] string Uid)
        {
            // Validate Uid parameter using SafeUidAttribute
            var uidValidationError = this.ValidateWithAttribute(
                Uid,
                new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1),
                memberName: "Uid",
                statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            var collection = await _service.GetCollectionByUidAsync(Uid);
            if (collection == null)
                return NotFound();
            return Ok(collection);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<BookmarkCollectionResponse>>> SearchCollections([FromQuery] string searchTerm)
        {
            // Validate searchTerm to prevent script injections or invalid names
            var nameValidationError = this.ValidateWithAttribute(
                searchTerm,
                new SafeNameAttribute(allowNullValue: false, maxLength: 50, minLength: 0),
                memberName: "searchTerm",
                statusCode: 400);
            if (nameValidationError != null) return nameValidationError;

            var user = await _currentUserService.GetUserAsync();
            var results = await _service.SearchCollectionsAsync(searchTerm ?? "");
            return Ok(results);
        }

        [HttpGet]
        public async Task<ActionResult<List<BookmarkCollectionResponse>>> GetAllCollections([FromQuery] string username = null)
        {
            var user = await _currentUserService.GetUserAsync();
            var results = new List<BookmarkCollectionResponse>();
            if (username != null)
            {
                results = await _service.GetAllCollectionsWithPostsAsync(username);
            }
            else 
            {
                results = await _service.GetAllCollectionsWithPostsAsync(user.Profile.Uid);
            }
            return Ok(results);
        }
    }
} 