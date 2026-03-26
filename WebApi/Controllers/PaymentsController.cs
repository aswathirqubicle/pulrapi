using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ApiControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ICurrentUserService _currentUserService;

    public PaymentsController(IStripeService stripeService, ICurrentUserService currentUserService)
    {
        _stripeService = stripeService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Create a payment for the current user with checkout summary.
    /// - If PaymentMethodId is provided, charges the saved card immediately (off-session).
    /// - If PaymentMethodId is null/empty, returns client secrets for PaymentSheet / mobile element.
    /// - Always builds and returns checkout summary (products, shipping, card details, order ID).
    /// - On payment success: Success = true, CheckoutSummary included.
    /// - On payment failure: Success = false, Error message, CheckoutSummary still included.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CreatePaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (!_currentUserService.IsUserLoggedIn())
        {
            return Unauthorized(new CreatePaymentResponse
            {
                Success = false,
                Error = "User must be logged in to process payment."
            });
        }

        var command = new Core.Application.Mediatr.Payments.Commands.Create.CreatePaymentCommand
        {
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethodId = request.PaymentMethodId,
            Note = request.Note,
            Products = request.Products,
            ShippingDetailsUid = request.ShippingDetailsUid,
            BillingDetailsUid = request.BillingAddressDetailsUid,
            ReturnUrl = request.ReturnUrl
        };

        var result = await Mediator.Send(command);

        if (!result.Success)
        {
            if (result.Error?.Contains("Authentication") == true || result.Error?.Contains("Unauthorized") == true)
            {
                return Unauthorized(result);
            }
            
            if (result.Error?.Contains("not found") == true || result.Error?.Contains("required") == true)
            {
                return BadRequest(result);
            }

            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost("customer-session")]
    [AllowAnonymous]
    public async Task<ActionResult<CreateCustomerSessionResponse>> CreateCustomerSession(
        [FromBody] CreateCustomerSessionRequest? request = null)
    {
        try
        {
            request ??= new CreateCustomerSessionRequest();
            var response = await _stripeService.CreateCustomerSessionAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Stripe customer details for the logged-in user.
    /// Uses the logged-in user's StripeCustomerId from database.
    /// </summary>
    [HttpGet("customer")]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerResponse>> GetCustomer()
    {
        try
        {
            var response = await _stripeService.GetCustomerAsync();
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            if (ex.StripeError?.Code == "resource_missing")
            {
                return NotFound(new { error = "Customer not found." });
            }
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("payment-methods")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodResponse>>> GetSavedPaymentMethods()
    {
        try
        {
            var response = await _stripeService.GetSavedPaymentMethodsAsync();
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("save-card")]
    [AllowAnonymous]
    public async Task<ActionResult<SaveCardResponse>> SaveCardDirect([FromBody] SaveCardRequest request)
    {
        try
        {
            var response = await _stripeService.SaveCardAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SaveCardResponse 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
    }

    [HttpDelete("payment-methods/{paymentMethodId}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> RemovePaymentMethod(string paymentMethodId)
    {
        if (!_currentUserService.IsUserLoggedIn())
        {
            return Unauthorized(new { error = "User must be logged in to remove a payment method." });
        }

        var command = new Core.Application.Mediatr.Payments.Commands.Delete.RemovePaymentMethodCommand
        {
            PaymentMethodId = paymentMethodId
        };

        var result = await Mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { error = "Failed to remove payment method." });
        }

        return Ok(result);
    }

    [HttpPut("payment-methods/{paymentMethodId}/default")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> SetDefaultPaymentMethod(string paymentMethodId)
    {
        if (!_currentUserService.IsUserLoggedIn())
        {
            return Unauthorized(new { error = "User must be logged in to set a default payment method." });
        }

        var command = new Core.Application.Mediatr.Payments.Commands.Update.SetDefaultPaymentMethodCommand
        {
            PaymentMethodId = paymentMethodId
        };

        var result = await Mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { error = "Failed to set default payment method." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a SetupIntent for saving a card from the frontend.
    /// Returns a client secret that can be used with Stripe Elements to collect card details.
    /// Requires authentication.
    /// </summary>
    [HttpPost("setup-intent")]
    [AllowAnonymous]
    public async Task<ActionResult<SetupIntentResponse>> CreateSetupIntent()
    {
        if (!_currentUserService.IsUserLoggedIn())
        {
            return Unauthorized(new { error = "User must be logged in to create a setup intent." });
        }

        try
        {
            var response = await _stripeService.CreateSetupIntentAsync();
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }


}
