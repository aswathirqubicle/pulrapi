using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Wallet;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Wallet.Queries
{
    public class GetWalletBalanceQuery : IRequest<WalletBalanceResponse>
    {
    }

    // Keyless row for reading the stored AspNetUsers balance columns via raw SQL.
    // These columns are not mapped on the EF User entity.
    public record UserBalanceRow(decimal AvailableBalance, decimal EscrowBalance);

    public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetWalletBalanceQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetWalletBalanceQueryHandler(
            IApplicationDbContext dbContext,
            ILogger<GetWalletBalanceQueryHandler> logger,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<WalletBalanceResponse> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(skipDetails: true);
                if (user == null)
                {
                    throw new NotAuthenticatedException("User must be logged in.");
                }

                // Read the stored running-total balances from AspNetUsers. These columns
                // (maintained by the escrow-release payout job, withdrawal flow, and refund
                // handling) are the authoritative available/in-escrow amounts. They are not
                // mapped on the EF User entity, so read them via raw SQL.
                var row = await _dbContext.Database
                    .SqlQuery<UserBalanceRow>(
                        $"SELECT \"AvailableBalance\", \"EscrowBalance\" FROM \"AspNetUsers\" WHERE \"Id\" = {user.Id}")
                    .FirstOrDefaultAsync(cancellationToken);

                var available = row?.AvailableBalance ?? 0m;
                var escrow = row?.EscrowBalance ?? 0m;

                // Get user's default currency (or use AED as default)
                var defaultCurrency = await _dbContext.GlobalCurrencySettings
                    .Include(g => g.BaseCurrency)
                    .FirstOrDefaultAsync(cancellationToken);

                return new WalletBalanceResponse
                {
                    AvailableBalance = available,
                    EscrowBalance = escrow,
                    TotalBalance = available + escrow,
                    CurrencyCode = defaultCurrency?.BaseCurrency?.Code ?? "AED"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wallet balance: {Message}", ex.Message);
                throw;
            }
        }
    }
}
