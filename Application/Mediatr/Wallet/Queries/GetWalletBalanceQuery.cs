using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Wallet.Queries
{
    public class GetWalletBalanceQuery : IRequest<WalletBalanceResponse>
    {
    }

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

                var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
                if (profile == null)
                {
                    throw new NotFoundException("Profile not found.");
                }

                // Calculate balance dynamically from completed Refund transactions only
                // Wallet is only affected by refunds, not by purchases, sales, or other transaction types
                var balance = await _dbContext.WalletTransactions
                    .Where(t => t.IsActive && t.ProfileId == profile.Id && t.Status == Core.Domain.Enums.TransactionStatusEnum.Completed)
                    .Where(t => t.TransactionType == Core.Domain.Enums.TransactionTypeEnum.Refund)
                    .SumAsync(t => t.Amount, cancellationToken);

                // Get user's default currency (or use AED as default)
                var defaultCurrency = await _dbContext.GlobalCurrencySettings
                    .Include(g => g.BaseCurrency)
                    .FirstOrDefaultAsync(cancellationToken);

                return new WalletBalanceResponse
                {
                    AvailableBalance = balance,
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
