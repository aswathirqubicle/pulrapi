using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Orders;
using Core.Application.Models.Wallet;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Wallet.Queries
{
    public class GetTransactionSummaryQuery : IRequest<TransactionSummaryResponse>
    {
        public string Uid { get; set; }
    }

    public class GetTransactionSummaryQueryHandler : IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetTransactionSummaryQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetTransactionSummaryQueryHandler(
            IApplicationDbContext dbContext,
            ILogger<GetTransactionSummaryQueryHandler> logger,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TransactionSummaryResponse> Handle(GetTransactionSummaryQuery request, CancellationToken cancellationToken)
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

                var transaction = await _dbContext.WalletTransactions
                    .Include(t => t.Currency)
                    .Include(t => t.Profile)
                        .ThenInclude(p => p.User)
                    .Include(t => t.Order)
                        .ThenInclude(o => o.OrderProductAffiliates)
                            .ThenInclude(opa => opa.Product)
                                .ThenInclude(p => p.User)
                    .Include(t => t.Order)
                        .ThenInclude(o => o.OrderProductAffiliates)
                            .ThenInclude(opa => opa.Product)
                                .ThenInclude(p => p.ProductMediaFiles)
                    .Include(t => t.Order)
                        .ThenInclude(o => o.Profile)
                            .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(t => t.Uid == request.Uid && t.ProfileId == profile.Id && t.IsActive, cancellationToken);

                if (transaction == null)
                {
                    throw new NotFoundException("Transaction not found.");
                }

                // Mask card number (e.g., "4034 87** **** 2874")
                string maskedCardNumber = null;
                if (!string.IsNullOrEmpty(transaction.CardNumberLast4))
                {
                    maskedCardNumber = $"**** **** **** {transaction.CardNumberLast4}";
                }

                // Get seller username instead of displayname
                string sellerName = null;
                if (transaction.TransactionType == TransactionTypeEnum.Purchase && transaction.Order?.OrderProductAffiliates != null)
                {
                    // For Purchase transactions (buyer viewing), get seller's username from order products
                    var sellerUser = transaction.Order.OrderProductAffiliates
                        .Where(opa => opa.Product?.User != null)
                        .Select(opa => opa.Product.User)
                        .FirstOrDefault();
                    
                    if (sellerUser != null)
                    {
                        sellerName = sellerUser.UserName;
                    }
                }
                else if (transaction.TransactionType == TransactionTypeEnum.Sale)
                {
                    // For Sale transactions (seller viewing), get seller's username from transaction's profile
                    // The seller is the one who owns this transaction
                    if (transaction.Profile?.User != null)
                    {
                        sellerName = transaction.Profile.User.UserName;
                    }
                    else
                    {
                        sellerName = transaction.SellerName;
                    }
                }
                else if (transaction.TransactionType == TransactionTypeEnum.ExchangeCharge ||
                         transaction.TransactionType == TransactionTypeEnum.ExchangeCredit)
                {
                    // Exchange transactions can belong to either the buyer or the seller,
                    // so resolve the role via the order's owning profile rather than the type.
                    if (transaction.Order != null && transaction.ProfileId == transaction.Order.ProfileId)
                    {
                        var sellerUser = transaction.Order.OrderProductAffiliates?
                            .Where(opa => opa.Product?.User != null)
                            .Select(opa => opa.Product.User)
                            .FirstOrDefault();

                        sellerName = sellerUser?.UserName;
                    }
                    else
                    {
                        sellerName = transaction.Profile?.User?.UserName ?? transaction.SellerName;
                    }
                }
                else
                {
                    // For other transaction types, use stored SellerName
                    sellerName = transaction.SellerName;
                }

                return new TransactionSummaryResponse
                {
                    Uid = transaction.Uid,
                    TransactionType = transaction.TransactionType.ToString(),
                    Amount = transaction.Amount,
                    CurrencyCode = transaction.Currency?.Code ?? "AED",
                    TransactionDate = transaction.TransactionDate,
                    Status = transaction.Status.ToString(),
                    CardUsed = transaction.CardType,
                    CardNumber = maskedCardNumber,
                    InitiationDate = transaction.CreatedAt,
                    OrderNumber = transaction.Order?.Uid,
                    SellerName = sellerName,
                    CollabId = transaction.Order?.CollabId,
                    PaymentBreakdown = transaction.Order != null
                        ? PaymentBreakdownResponse.Build(
                            transaction.Order.VatAmount,
                            transaction.Order.OrderProductAffiliates,
                            isBuyer: transaction.TransactionType == TransactionTypeEnum.Purchase)
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction summary for UID {Uid}: {Message}", request.Uid, ex.Message);
                throw;
            }
        }
    }
}
