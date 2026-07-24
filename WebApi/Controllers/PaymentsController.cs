using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Stripe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ApiControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IStripeService stripeService, ICurrentUserService currentUserService, ILogger<PaymentsController> logger)
    {
        _stripeService = stripeService;
        _currentUserService = currentUserService;
        _logger = logger;
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
    [Authorize]
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
            ReturnUrl = request.ReturnUrl,
            IsExchange = request.IsExchange,
            ExchangeOrderUid = request.ExchangeOrderUid,
            ExchangeItems = request.ExchangeItems ?? new()
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
    [Authorize]
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
            _logger.LogError(ex, "Error creating customer session");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    /// <summary>
    /// Get Stripe customer details for the logged-in user.
    /// Uses the logged-in user's StripeCustomerId from database.
    /// </summary>
    [HttpGet("customer")]
    [Authorize]
    public async Task<ActionResult<CustomerResponse>> GetCustomer()
    {
        try
        {
            var response = await _stripeService.GetCustomerAsync();
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting customer");
            return Unauthorized(new { error = "Unauthorized." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad argument getting customer");
            return BadRequest(new { error = "Invalid request." });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting customer");
            if (ex.StripeError?.Code == "resource_missing")
            {
                return NotFound(new { error = "Customer not found." });
            }
            return StatusCode(500, new { error = "A payment service error occurred." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting customer");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpGet("payment-methods")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodResponse>>> GetSavedPaymentMethods()
    {
        try
        {
            var response = await _stripeService.GetSavedPaymentMethodsAsync();
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting payment methods");
            return Unauthorized(new { error = "Unauthorized." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad argument getting payment methods");
            return BadRequest(new { error = "Invalid request." });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error getting payment methods");
            return StatusCode(500, new { error = "A payment service error occurred." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting payment methods");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("save-card")]
    [Authorize]
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
            _logger.LogError(ex, "Unexpected error saving card");
            return StatusCode(500, new SaveCardResponse
            {
                Success = false,
                Error = "An unexpected error occurred."
            });
        }
    }

    [HttpDelete("payment-methods/{paymentMethodId}")]
    [Authorize]
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
    [Authorize]
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
    [Authorize]
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
            _logger.LogWarning(ex, "Unauthorized access creating setup intent");
            return Unauthorized(new { error = "Unauthorized." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating setup intent");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }


}
