using System;
using System.Threading.Tasks;
using Core.Application.DTOs;
using Core.Application.Exceptions;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Mediatr.Stores.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SellerSettingsController : ApiControllerBase
    {
        private readonly ILogger<SellerSettingsController> _logger;

        public SellerSettingsController(ILogger<SellerSettingsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<SellerSettingsDto>> GetSellerSettings()
        {
            var result = await Mediator.Send(new GetSellerSettingsQuery());
            return Ok(result);
        }

        [HttpPut]
        public async Task<ActionResult<SellerSettingsDto>> UpdateSellerSettings([FromBody] UpdateSellerSettingsDto settings)
        {
            var command = new UpdateSellerSettingsCommand
            {
                PhoneNumber = settings.PhoneNumber,
                Email = settings.Email,
                ShippingCosts = settings.ShippingCosts,
                DeliveryTime = settings.DeliveryTime,
                ExchangePolicy = settings.ExchangePolicy,
                RefundPolicy = settings.RefundPolicy
            };
            
            var result = await Mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("send-email-otp")]
        public async Task<ActionResult<SendSellerEmailOtpResponse>> SendEmailOtp([FromBody] SendSellerEmailOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending seller email OTP");
                return StatusCode(500, new { Message = "An error occurred while sending the OTP." });
            }
        }

        [HttpPost("verify-email-otp")]
        public async Task<ActionResult<VerifySellerEmailOtpResponse>> VerifyEmailOtp([FromBody] VerifySellerEmailOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying seller email OTP");
                return StatusCode(500, new { Message = "An error occurred while verifying the OTP." });
            }
        }

    }
}


