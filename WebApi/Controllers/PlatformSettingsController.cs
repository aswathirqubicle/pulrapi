using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.PaymentFeeSettings.Commands;
using Core.Application.Mediatr.PaymentFeeSettings.Queries;
using Core.Application.Mediatr.PlatformSettings.Commands;
using Core.Application.Mediatr.PlatformSettings.Queries;
using Core.Application.Models.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    public class PlatformSettingsController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PlatformSettingResponse>> GetPlatformSettings()
        {
            var setting = await Mediator.Send(new GetPlatformSettingsQuery());
            if (setting == null)
                return NotFound("Platform settings not found");

            return Ok(new PlatformSettingResponse
            {
                Uid = setting.Uid,
                CommissionRate = setting.CommissionRate,
                VatRate = setting.VatRate,
                PlatformFeePercentage = setting.PlatformFeePercentage,
                DirectSaleSellerPercentage = setting.DirectSaleSellerPercentage,
                CollabSaleSellerPercentage = setting.CollabSaleSellerPercentage,
                CollabSaleCreatorPercentage = setting.CollabSaleCreatorPercentage,
                MinimumWithdrawalAmount = setting.MinimumWithdrawalAmount,
                DeliveryExtensionHours = setting.DeliveryExtensionHours,
                RefundWindowDays = setting.RefundWindowDays,
                ExchangeWindowDays = setting.ExchangeWindowDays,
                EscrowHoldDays = setting.EscrowHoldDays
            });
        }

        [HttpPut]
        [Authorize(Roles = PulrRoles.Administrator)]
        public async Task<ActionResult<PlatformSettingResponse>> UpdatePlatformSettings([FromBody] UpdatePlatformSettingsCommand command)
        {
            var setting = await Mediator.Send(command);
            return Ok(new PlatformSettingResponse
            {
                Uid = setting.Uid,
                CommissionRate = setting.CommissionRate,
                VatRate = setting.VatRate,
                PlatformFeePercentage = setting.PlatformFeePercentage,
                DirectSaleSellerPercentage = setting.DirectSaleSellerPercentage,
                CollabSaleSellerPercentage = setting.CollabSaleSellerPercentage,
                CollabSaleCreatorPercentage = setting.CollabSaleCreatorPercentage,
                MinimumWithdrawalAmount = setting.MinimumWithdrawalAmount,
                DeliveryExtensionHours = setting.DeliveryExtensionHours,
                RefundWindowDays = setting.RefundWindowDays,
                ExchangeWindowDays = setting.ExchangeWindowDays,
                EscrowHoldDays = setting.EscrowHoldDays
            });
        }

        [HttpGet("payment-fee")]
        public async Task<ActionResult<List<PaymentFeeSettingResponse>>> GetPaymentFeeSettings()
        {
            return Ok(await Mediator.Send(new GetPaymentFeeSettingsQuery()));
        }

        [HttpPost("payment-fee")]
        [Authorize(Roles = PulrRoles.Administrator)]
        public async Task<ActionResult<PaymentFeeSettingResponse>> CreatePaymentFeeSetting([FromBody] CreatePaymentFeeSettingCommand command)
        {
            var result = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetPaymentFeeSettings), result);
        }

        [HttpPut("payment-fee/{uid}")]
        [Authorize(Roles = PulrRoles.Administrator)]
        public async Task<ActionResult<PaymentFeeSettingResponse>> UpdatePaymentFeeSetting(string uid, [FromBody] UpdatePaymentFeeSettingCommand command)
        {
            command.Uid = uid;
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }
}