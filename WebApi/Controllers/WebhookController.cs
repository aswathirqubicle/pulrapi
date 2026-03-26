using System;
using System.IO;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IStripeService stripeService, ILogger<WebhookController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"];

        if (string.IsNullOrEmpty(stripeSignature))
        {
            _logger.LogWarning("Stripe-Signature header is missing.");
            return BadRequest();
        }

        try
        {
            var result = await _stripeService.HandleWebhookAsync(json, stripeSignature);
            if (!result)
            {
                return BadRequest();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook.");
            return StatusCode(500);
        }
    }
}
