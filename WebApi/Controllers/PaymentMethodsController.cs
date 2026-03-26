using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Core.Application.Mediatr.PaymentMethods.Queries;
using System.Collections.Generic;
using Core.Application.Models.PaymentMethods;

namespace WebApi.Controllers;

#if DISABLED

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodsController : ApiControllerBase
    {
        // TODO test
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<PaymentMethodResponse>>> GetPaymentMethods()
        {
            var res = await Mediator.Send(new GetPaymentMethodsQuery());
            return Ok(res);
        }
    }

#endif