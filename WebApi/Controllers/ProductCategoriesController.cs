using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Core.Application.Mediatr.ProductCategories.Commands;
using Core.Application.Mediatr.ProductCategories.Queries;
using Core.Application.Models.Products;
using Core.Application.Models;
using Core.Application.Models.Categories;

namespace WebApi.Controllers;
#if DISABLED
public class ProductCategoriesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("all/{storeUid}")]
    public async Task<ActionResult<List<ProductCategoryResponse>>> GetCategories(string storeUid)
    {
        var res = await Mediator.Send(new GetCategoriesQuery()
            { StoreUid = storeUid });
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateCategory(CreateProductCategoryCommand request)
    {
        var uid = await Mediator.Send(request);
        return Ok(new UidBaseResponse () { Uid = uid });
    }

    [HttpPut]
    public async Task<ActionResult> UpdateCategory(UpdateProductCategoryCommand request)
    {
        await Mediator.Send(request);
        return Ok();
    }

    [HttpDelete("{uid}")]
    public async Task<ActionResult> DeleteCategory(string uid)
    {
        await Mediator.Send(new DeleteProductCategoryCommand() { Uid = uid });
        return Ok();
    }

    [AllowAnonymous]
    [HttpGet("main-categories")]
    public async Task<ActionResult<List<CategoryResponse>>> GetAllMainCategories()
    {
        var res = await Mediator.Send(new GetAllMainCategoriesQuery());
        return Ok(res);
    }
}
#endif