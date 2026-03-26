using Core.Application.Mediatr.Currencies.Queries;
using Core.Application.Models.Currencies;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers;
#if DISABLED
public class CurrenciesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("{uid}")]
    public async Task<ActionResult<CurrencyDetailsResponse>> GetCurrency(string uid)
    {
        var uidValidationError = this.ValidateWithAttribute(
        uid,
        new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
        memberName: "Uid",
        statusCode: 400);
        if (uidValidationError != null) return uidValidationError;
        var res = await Mediator.Send(new GetCurrencyQuery() { Uid = uid });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<AllCurrenciesResponse>> GetCurrencies()
    {
        var res = await Mediator.Send(new GetCurrenciesQuery());
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet("global")]
    public async Task<ActionResult<CurrencyDetailsResponse>> GetGlobalCurrency()
    {
        var res = await Mediator.Send(new GetGlobalCurrencyQuery());
        return Ok(res);
    }

}
#endif
