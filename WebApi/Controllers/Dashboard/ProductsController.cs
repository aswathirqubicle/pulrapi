using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Application.Models.Products;
using Dashboard.Application.Mediatr.Products.Commands.Create;
using Dashboard.Application.Mediatr.Products.Commands.Delete;
using Dashboard.Application.Mediatr.Products.Commands.Update;
using Dashboard.Application.Mediatr.Products.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Dashboard;
#if DISABLED
public class ProductsController : DashboardApiControllerBase
{
    [HttpGet("{productUid}")]
    public async Task<ActionResult<ProductDetailsResponse>> GetProduct(string productUid)
    {
        var res = await Mediator.Send(new GetProductQuery() { ProductUid = productUid });
        return Ok(res);
    }

    [HttpGet("inventory/{storeUid?}")]
    public async Task<ActionResult<PagingResponse<ProductInventoryResponse>>> GetProductsInventory([FromQuery] GetProductsInventoryQuery query, string storeUid)
    {
        query.StoreUid = storeUid;
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var uid = await Mediator.Send(command);
        return Ok(new UidBaseResponse() { Uid = uid });
    }

    [HttpPut]
    public async Task<ActionResult> UpdateProduct([FromBody] UpdateProductCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpDelete("{productUid}")]
    public async Task<ActionResult> DeleteProduct(string productUid)
    {
        await Mediator.Send(new DeleteProductCommand { ProductUid = productUid });
        return NoContent();
    }

    [HttpPut("images")]
    public async Task<ActionResult<List<ProductImageUpdateResponse>>> UpdateProductImages([FromForm] UpdateProductImagesCommand command)
    {
        var res = await Mediator.Send(command);
        return Ok(res);
    }

    [HttpDelete("{productUid}/image/{imageUid}")]
    public async Task<ActionResult> DeleteProductImage(string productUid, string imageUid)
    {
        await Mediator.Send(new DeleteProductImageCommand { ProductUid = productUid, ImageUid = imageUid });
        return NoContent();
    //}
}
#endif