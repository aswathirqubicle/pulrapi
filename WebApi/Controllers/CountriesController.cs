using Core.Application.Mediatr.Country.Queries;
using Core.Application.Models.Country;
using Core.Application.Security.Validation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    public class CountriesController : ApiControllerBase
    {
        [HttpGet("{uid}")]
        public async Task<ActionResult<CountryDetailsResponse>> Get(string uid)
        {
            var uidValidationError = this.ValidateWithAttribute(
            uid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "Uid",
            statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            var res = await Mediator.Send(new GetCountryQuery() { Uid = uid });
            return Ok(res);
        }

        [HttpGet]
        public async Task<ActionResult<List<CountryResponse>>> GetCountries()
        {
            var res = await Mediator.Send(new GetCountriesQuery());
            return Ok(res);
        }

        // TODO
        //[HttpGet("{countryUid}/cities")]
        //public async Task<ActionResult<List<CityResponse>>> GetCitiesByCountry(string countryUid)
        //{
        //    var res = await Mediator.Send(new GetCitiesByCountryQuery() { CountryUid = countryUid });
        //    return Ok(res);
        //}
    }
}
