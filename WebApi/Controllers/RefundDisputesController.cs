using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Admin.Queries.GetRefundDisputes;
using Core.Application.Mediatr.Admin.Queries.GetRefundDisputeDetail;
using Core.Application.Mediatr.Admin.Commands.ResolveRefundDispute;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = PulrRoles.Administrator + "," + PulrRoles.Moderator)]
    public class RefundDisputesController : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<RefundDisputeSummaryDto>>> GetRefundDisputes([FromQuery] DisputeStatusEnum? status)
        {
            var query = new GetRefundDisputesQuery { Status = status };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{uid}")]
        public async Task<ActionResult<RefundDisputeDetailDto>> GetRefundDisputeDetail(string uid)
        {
            var query = new GetRefundDisputeDetailQuery { DisputeUid = uid };
            var result = await Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("{uid}/resolve")]
        public async Task<ActionResult<ResolveRefundDisputeResponse>> ResolveRefundDispute(string uid, [FromBody] ResolveRefundDisputeRequest request)
        {
            var command = new ResolveRefundDisputeCommand
            {
                DisputeUid = uid,
                Decision = request.Decision,
                Notes = request.Notes,
                NetRefundAmount = request.NetRefundAmount
            };
            var result = await Mediator.Send(command);
            return Ok(result);
        }
    }

    public class ResolveRefundDisputeRequest
    {
        [Required]
        public string Decision { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        public decimal? NetRefundAmount { get; set; }
    }
}
