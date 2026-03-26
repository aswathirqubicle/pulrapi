using System.Threading.Tasks;
using Core.Application.Mediatr.Wallet.Queries;
using Core.Application.Models;
using Core.Application.Models.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ApiControllerBase
{
    /// <summary>
    /// Get wallet balance for the logged-in user.
    /// Balance is calculated dynamically from all completed transactions.
    /// </summary>
    [HttpGet("balance")]
    [Authorize]
    public async Task<ActionResult<WalletBalanceResponse>> GetWalletBalance()
    {
        var res = await Mediator.Send(new GetWalletBalanceQuery());
        return Ok(res);
    }

    /// <summary>
    /// Get transaction history for the logged-in user.
    /// Supports filtering by transaction direction: All, In (credits), Out (debits).
    /// Returns paginated list of transactions ordered by date (newest first).
    /// </summary>
    [HttpGet("transactions")]
    [Authorize]
    public async Task<ActionResult<PagingResponse<WalletTransactionResponse>>> GetTransactionHistory(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string filter = "All")
    {
        var res = await Mediator.Send(new GetTransactionHistoryQuery 
        { 
            PageNumber = pageNumber, 
            PageSize = pageSize,
            Filter = filter
        });
        return Ok(res);
    }

    /// <summary>
    /// Get detailed transaction summary by UID for the logged-in user.
    /// Includes card information, seller details, and order reference.
    /// </summary>
    [HttpGet("transactions/{uid}")]
    [Authorize]
    public async Task<ActionResult<TransactionSummaryResponse>> GetTransactionSummary(string uid)
    {
        var res = await Mediator.Send(new GetTransactionSummaryQuery { Uid = uid });
        return Ok(res);
    }

    /// <summary>
    /// Create a dispute for a wallet transaction.
    /// User must provide contact details (email, phone) and a description of the issue.
    /// Returns confirmation with dispute UID and status.
    /// </summary>
    [HttpPost("disputes")]
    [Authorize]
    public async Task<ActionResult<DisputeResponse>> CreateDispute([FromBody] DisputeRequest request)
    {
        var res = await Mediator.Send(new Core.Application.Mediatr.Wallet.Commands.Create.CreateDisputeCommand 
        { 
            Request = request 
        });
        return Ok(res);
    }
}
