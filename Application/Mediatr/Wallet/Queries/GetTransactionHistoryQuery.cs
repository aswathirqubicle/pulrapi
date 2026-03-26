using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Wallet;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Wallet.Queries
{
    public class GetTransactionHistoryQuery : PagingParamsRequest, IRequest<PagingResponse<WalletTransactionResponse>>
    {
        /// <summary>
        /// Filter: "All", "In" (credits), "Out" (debits)
        /// </summary>
        public string Filter { get; set; } = "All";
    }

    public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, PagingResponse<WalletTransactionResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetTransactionHistoryQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetTransactionHistoryQueryHandler(
            IApplicationDbContext dbContext,
            ILogger<GetTransactionHistoryQueryHandler> logger,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<WalletTransactionResponse>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
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

                var transactionsQuery = _dbContext.WalletTransactions
                    .Include(t => t.Currency)
                    .Include(t => t.Order)
                        .ThenInclude(o => o.OrderProductAffiliates)
                            .ThenInclude(opa => opa.Product)
                                .ThenInclude(p => p.User)
                    .Include(t => t.OrderProductAffiliate)
                    .Where(t => t.IsActive && t.ProfileId == profile.Id);

                // Show Purchase and Refund transactions
                transactionsQuery = transactionsQuery.Where(t =>
                    t.TransactionType == TransactionTypeEnum.Purchase ||
                    t.TransactionType == TransactionTypeEnum.Refund);

                // Apply filter (currently disabled - only Purchase transactions are shown)
                // if (request.Filter?.ToLower() == "in")
                // {
                //     // Credits: Sale, Refund, Commission, ExchangeCredit
                //     transactionsQuery = transactionsQuery.Where(t => 
                //         t.TransactionType == TransactionTypeEnum.Sale ||
                //         t.TransactionType == TransactionTypeEnum.Refund ||
                //         t.TransactionType == TransactionTypeEnum.Commission ||
                //         t.TransactionType == TransactionTypeEnum.ExchangeCredit);
                // }
                // else if (request.Filter?.ToLower() == "out")
                // {
                //     // Debits: Purchase, ExchangeCharge
                //     transactionsQuery = transactionsQuery.Where(t => 
                //         t.TransactionType == TransactionTypeEnum.Purchase ||
                //         t.TransactionType == TransactionTypeEnum.ExchangeCharge);
                // }

                transactionsQuery = transactionsQuery.OrderByDescending(t => t.TransactionDate);

                var totalCount = await transactionsQuery.CountAsync(cancellationToken);
                var transactions = await transactionsQuery
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Get all unique seller user IDs from the transactions
                var sellerUserIds = transactions
                    .Where(t => t.Order?.OrderProductAffiliates != null)
                    .SelectMany(t => t.Order.OrderProductAffiliates)
                    .Where(opa => opa.Product?.UserId != null)
                    .Select(opa => opa.Product.UserId)
                    .Distinct()
                    .ToList();

                // Fetch stores for all sellers
                var sellerStores = await _dbContext.Stores
                    .Where(s => sellerUserIds.Contains(s.UserId) && s.IsActive)
                    .ToDictionaryAsync(s => s.UserId, s => s.Name, cancellationToken);

                var items = transactions.Select(t => 
                {
                    var sellerNames = new List<string>();
                    
                    if (t.Order?.OrderProductAffiliates != null)
                    {
                        // Get unique seller names from the order
                        sellerNames = t.Order.OrderProductAffiliates
                            .Where(opa => opa.Product?.UserId != null)
                            .Select(opa => 
                            {
                                var sellerId = opa.Product.UserId;
                                var firstName = opa.Product.User?.FirstName;
                                
                                // Return FirstName if available, otherwise "Unknown Seller"
                                // We are prioritizing the person's name as requested
                                return !string.IsNullOrWhiteSpace(firstName) 
                                    ? firstName 
                                    : (opa.Product.User?.FirstName ?? "Unknown Seller");
                            })
                            .Distinct()
                            .ToList();
                    }

                    return new WalletTransactionResponse
                    {
                        Uid = t.Uid,
                        OrderUid = t.Order?.Uid,
                        OrderItemUid = t.OrderProductAffiliate?.Uid,
                        TransactionType = t.TransactionType.ToString(),
                        Amount = t.Amount,
                        CurrencyCode = t.Currency?.Code ?? "AED",
                        Description = t.Description,
                        TransactionDate = t.TransactionDate,
                        PlacementDate = t.Order?.CreatedAt,
                        Status = t.Status.ToString(),
                        CardNumberLast4 = t.CardNumberLast4,
                        CardType = t.CardType,
                        SellerNames = sellerNames
                    };
                }).ToList();

                return new PagingResponse<WalletTransactionResponse>
                {
                    Items = items,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    ItemIds = items.Select(i => i.Uid).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction history: {Message}", ex.Message);
                throw;
            }
        }
    }
}
