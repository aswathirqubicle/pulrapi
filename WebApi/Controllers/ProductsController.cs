using Core.Application.Constants;
using Core.Application.Mediatr.Categories.Queries;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly IConfiguration _configuration;

    public ProductsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    [AllowAnonymous]
    [HttpGet("{uid}")]
    public async Task<ActionResult<ProductDetailsResponse>> GetProductDetails(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
        uid,
        new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
        memberName: "uid",
        statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        var res = await Mediator.Send(new GetProductDetailsQuery() { Uid = uid });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagingResponse<ProductPublicResponse>>> GetPublicProducts([FromQuery] GetPublicProductsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpGet("my")]
    public async Task<ActionResult<PagingResponse<ProductDetailsResponse>>> GetMyProducts([FromQuery] GetUserProductsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpGet("to-tag")]
    public async Task<ActionResult<PagingResponse<ProductPublicResponse>>> GetProductsToTagPaged([FromQuery] ProductsToTagListQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    //[AllowAnonymous]
    //[HttpGet("featured")]
    //public async Task<ActionResult<PagingResponse<ProductPublicResponse>>> GetFeaturedProducts([FromQuery] GetFeaturedProductsQuery query)
    //{
    //    var res = await Mediator.Send(query);
    //    return Ok(res);
    //}

    [AllowAnonymous]
    [HttpGet("featured")]
    public async Task<ActionResult<FeaturedProductsResponse>> GetFeaturedProducts([FromQuery] GetFeaturedProductsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    //[AllowAnonymous]
    //[HttpGet("similar-products/{productUid}")]
    //public async Task<ActionResult<ProductSimilarsResponse>> GetSimilarProduct(string productUid)
    //{
    //    var res = await Mediator.Send(new ProductSimilarsQuery() { ProductUid = productUid });
    //    return Ok(res);
    //}

    //[Authorize(Roles = PulrRoles.StoreOwner)]
    //[HttpGet("inventory/{storeUid}")]
    //public async Task<ActionResult<PagingResponse<ProductInventoryResponse>>> GetInventory([FromQuery] PagingParamsRequest pagingParams, string storeUid)
    //{
    //    var res = await Mediator.Send(new GetProductInventoryQuery() { PagingParams = pagingParams, StoreUid = storeUid });
    //    return Ok(res);
    //}

    [Authorize(Roles = PulrRoles.User)]
    [HttpPost]
    public async Task<ActionResult<ProductDetailsResponse>> CreateProduct(ProductCreateCommand command)
    {
        var res = await Mediator.Send(command);
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpPut]
    public async Task<ActionResult<ProductDetailsResponse>> UpdateProduct(ProductUpdateCommand command)
    {
        var res = await Mediator.Send(command);
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpDelete("{uid}")]
    public async Task<ActionResult> DeleteProduct(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
            uid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "uid",
            statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        await Mediator.Send(new ProductDeleteCommand { Uid = uid });
        return NoContent();
    }

    //[Authorize(Roles = PulrRoles.User)]
    //[HttpPut("images")]
    //public async Task<ActionResult<List<ProductImageUpdateResponse>>> UpdateProductImages([FromForm] UpdateProductImagesCommand command)
    //{
    //    var res = await Mediator.Send(command);
    //    return Ok(res);
    //}

    //[Authorize(Roles = PulrRoles.User)]
    //[HttpDelete("{productUid}/image/{imageUid}")]
    //public async Task<ActionResult> DeleteProductImage(string productUid, string imageUid)
    //{
    //    await Mediator.Send(new DeleteProductImageCommand() { ProductUid = productUid, ImageUid = imageUid });
    //    return NoContent();
    //}

    //[Authorize(Roles = PulrRoles.User)]
    //[HttpPut("{productUid}/toggle-like")]
    //public async Task<ActionResult<ProductToggleLikeResponse>> ToggleProductLike(string productUid)
    //{
    //    var likedByMe = await Mediator.Send(new ToggleProductLikeCommand() { Uid = productUid });
    //    return Ok(new ProductToggleLikeResponse () { LikedByMe = likedByMe });
    //}

    //[Authorize(Roles = PulrRoles.StoreOwner)]
    //[HttpPost("preferences")]
    //public async Task<ActionResult> CreateProductPreferences(ProductPreferencesCreateCommand command)
    //{
    //    await Mediator.Send(command);
    //    return Ok();
    //}

    //[Authorize(Roles = PulrRoles.StoreOwner)]
    //[HttpPut("preferences")]
    //public async Task<ActionResult> UpdateProductPreferences(ProductPreferencesUpdateCommand command)
    //{
    //    await Mediator.Send(command);
    //    return Ok();
    //}

    //[Authorize(Roles = PulrRoles.StoreOwner)]
    //[HttpGet("preferences")]
    //public async Task<ActionResult<ProductOnboardingPreferencesResponse>> GetProductPreferences([FromQuery]GetProductOnboardingPreferencesQuery query)
    //{
    //    var res = await Mediator.Send(query);
    //    return Ok(res);
    //}

    [AllowAnonymous]
    [HttpGet("{uid}/tagged-posts")]
    public async Task<ActionResult<PagingResponse<PostDetailsResponse>>> GetProductTaggedPosts(
        string uid, 
        [FromQuery] string excludePostUid = null,
        [FromQuery] string currencyCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var uidValidationError = this.ValidateWithAttribute(
            uid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "uid",
            statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        var query = new GetPostsByTaggedProductQuery
        {
            ProductUid = uid,
            ExcludePostUid = excludePostUid,
            CurrencyCode = currencyCode,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<ActionResult<List<Core.Application.Mediatr.Categories.Queries.ProductCategoryResponse>>> GetProductCategories()
    {
        var res = await Mediator.Send(new GetProductCategoriesQuery());
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpPut("{uid}/click")]
    public async Task<ActionResult<ProductClickStatisticsResponse>> RecordProductClick(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
            uid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "uid",
            statusCode: 400);
        if (uidValidationError != null) return uidValidationError;

        var res = await Mediator.Send(new RecordProductClickCommand { ProductUid = uid });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpPost("dev/owner-statistics")]
    public async Task<ActionResult<ProductOwnerStatisticsListResponse>> GetProductOwnerStatistics(
        [FromBody] ProductOwnerStatisticsRequest request)
    {
        // Validate passcode
        var expectedPasscode = _configuration["DevAccess:Passcode"];
        if (string.IsNullOrEmpty(expectedPasscode) || request.Passcode != expectedPasscode)
        {
            return Unauthorized(new { message = "Invalid passcode." });
        }

        var res = await Mediator.Send(new GetProductOwnerStatisticsQuery());
        return Ok(res);
    }
}

public class ProductOwnerStatisticsRequest
{
    public string Passcode { get; set; }
}